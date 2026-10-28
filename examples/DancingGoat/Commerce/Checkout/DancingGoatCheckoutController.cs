using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CMS.Commerce;
using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Membership;

using DancingGoat;
using DancingGoat.Commerce;
using DancingGoat.Helpers;
using DancingGoat.Models;
using DancingGoat.Services;

using Kentico.Commerce.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.Membership;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;

[assembly: RegisterWebPageRoute(Checkout.CONTENT_TYPE_NAME, typeof(DancingGoatCheckoutController), WebsiteChannelNames = new[] { DancingGoatConstants.WEBSITE_CHANNEL_NAME })]

namespace DancingGoat.Commerce;

/// <summary>
/// Controller for managing the checkout process.
/// </summary>
public sealed class DancingGoatCheckoutController : Controller
{
    private readonly CountryStateRepository countryStateRepository;
    private readonly ICurrentShoppingCartRetriever currentShoppingCartRetriever;
    private readonly ICurrentShoppingCartDiscardHandler currentShoppingCartDiscardHandler;
    private readonly UserManager<ApplicationUser> userManager;
    private readonly CustomerDataRetriever customerDataRetriever;
    private readonly IPreferredLanguageRetriever currentLanguageRetriever;
    private readonly OrderService orderService;
    private readonly IStringLocalizer<SharedResources> localizer;
    private readonly ProductNameProvider productNameProvider;
    private readonly ProductRepository productRepository;
    private readonly PaymentRepository paymentRepository;
    private readonly ShippingRepository shippingRepository;
    private readonly CalculationService calculationService;
    private readonly PriceCalculationTotalsCalculator priceCalculationTotalsCalculator;
    private readonly ShippingMethodOptionsService shippingMethodOptionsService;
    private readonly IInfoProvider<OrderInfo> orderInfoProvider;

    public DancingGoatCheckoutController(
        CountryStateRepository countryStateRepository,
        ICurrentShoppingCartRetriever currentShoppingCartRetriever,
        ICurrentShoppingCartDiscardHandler currentShoppingCartDiscardHandler,
        UserManager<ApplicationUser> userManager,
        CustomerDataRetriever customerDataRetriever,
        IPreferredLanguageRetriever currentLanguageRetriever,
        OrderService orderService,
        IStringLocalizer<SharedResources> localizer,
        ProductNameProvider productNameProvider,
        ProductRepository productRepository,
        PaymentRepository paymentRepository,
        ShippingRepository shippingRepository,
        CalculationService calculationService,
        PriceCalculationTotalsCalculator priceCalculationTotalsCalculator,
        ShippingMethodOptionsService shippingMethodOptionsService,
        IInfoProvider<OrderInfo> orderInfoProvider)
    {
        this.countryStateRepository = countryStateRepository;
        this.currentShoppingCartRetriever = currentShoppingCartRetriever;
        this.currentShoppingCartDiscardHandler = currentShoppingCartDiscardHandler;
        this.userManager = userManager;
        this.customerDataRetriever = customerDataRetriever;
        this.currentLanguageRetriever = currentLanguageRetriever;
        this.orderService = orderService;
        this.localizer = localizer;
        this.productNameProvider = productNameProvider;
        this.productRepository = productRepository;
        this.paymentRepository = paymentRepository;
        this.shippingRepository = shippingRepository;
        this.calculationService = calculationService;
        this.priceCalculationTotalsCalculator = priceCalculationTotalsCalculator;
        this.shippingMethodOptionsService = shippingMethodOptionsService;
        this.orderInfoProvider = orderInfoProvider;
    }


    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(await GetCustomerStepViewModel(null, null, null, null, cancellationToken));
    }


    [HttpPost]
    public async Task<IActionResult> Index(CustomerViewModel customer, CustomerAddressViewModel billingAddress, ShippingAddressViewModel shippingAddress, PaymentShippingViewModel paymentShipping, CancellationToken cancellationToken)
    {
        if (!await IsValid(billingAddress, shippingAddress, cancellationToken))
        {
            return View(await GetCustomerStepViewModel(customer, billingAddress, shippingAddress, paymentShipping, cancellationToken));
        }

        var calculation = await CalculateCurrentShoppingCart(billingAddress, paymentShipping, cancellationToken);
        if (calculation == null)
        {
            ModelState.AddModelError(string.Empty, localizer["Order not created. The shopping cart could not be found."]);

            return View(await GetCheckoutViewModel(customer, billingAddress, shippingAddress,
                ShoppingCartViewModel.Empty, paymentShipping, 0, 0, false, cancellationToken));
        }

        // Items whose products were deleted or unpublished are dropped by the price calculation, so an order would be created without any items.
        if (!calculation.CalculationResult.Items.Any())
        {
            ModelState.AddModelError(string.Empty, localizer["Order not created. The shopping cart does not contain any available products."]);

            return View(await GetCheckoutViewModel(customer, billingAddress, shippingAddress,
                ShoppingCartViewModel.Empty, paymentShipping, 0, 0, false, cancellationToken));
        }

        var orderNumber = await PlaceOrder(customer, billingAddress, shippingAddress, paymentShipping, calculation, cancellationToken);

        return View("ConfirmOrder", new ConfirmOrderViewModel(orderNumber));
    }


    /// <summary>
    /// Re-renders the order summary card so the shipping and tax rows follow the currently
    /// selected shipping method and billing address without a full page post.
    /// </summary>
    [HttpPost]
    [Route("{languageName}/Checkout/GetOrderSummary")]
    public async Task<IActionResult> GetOrderSummary(CustomerAddressViewModel billingAddress, PaymentShippingViewModel paymentShipping, string languageName, CancellationToken cancellationToken)
    {
        HttpContext.Request.RouteValues.Add(WebPageRoutingOptions.LANGUAGE_ROUTE_VALUE_KEY, languageName);

        // The summary is re-rendered mid-edit; validation errors from the partially filled
        // checkout form must not surface in the summary card markup.
        ModelState.Clear();

        var calculation = await CalculateCurrentShoppingCart(billingAddress, paymentShipping, cancellationToken);
        if (calculation == null)
        {
            return PartialView("_OrderSummaryCard", new OrderSummaryViewModel(ShoppingCartViewModel.Empty, 0, 0, false));
        }

        return PartialView("_OrderSummaryCard", new OrderSummaryViewModel(calculation.ShoppingCart, calculation.CalculationResult.ShippingPrice, calculation.OriginalShippingPrice, calculation.HasFreeShippingPromotion));
    }


    [HttpPost]
    [Route("/Checkout/GetStates")]
    public async Task<IEnumerable<SelectListItem>> GetStates(int countryId, CancellationToken cancellationToken)
    {
        if (countryId > 0)
        {
            var states = await countryStateRepository.GetStates(countryId, cancellationToken);
            return states.Select(x => new SelectListItem()
            {
                Text = x.StateDisplayName,
                Value = x.StateID.ToString(),
            }).ToList();
        }
        return new List<SelectListItem>();
    }


    /// <summary>
    /// Creates the order from the calculated shopping cart, discards the cart and returns the new order number.
    /// </summary>
    /// <remarks>
    /// The shipping price must always come from the server-side calculation, never from client input.
    /// </remarks>
    private async Task<string> PlaceOrder(CustomerViewModel customer, CustomerAddressViewModel billingAddress, ShippingAddressViewModel shippingAddress,
        PaymentShippingViewModel paymentShipping, ShoppingCartCalculation calculation, CancellationToken cancellationToken)
    {
        var user = await GetAuthenticatedUser();
        var languageName = currentLanguageRetriever.Get();

        int.TryParse(paymentShipping.PaymentMethodId, out var paymentMethodId);
        int.TryParse(paymentShipping.ShippingMethodId, out var shippingMethodId);

        var orderId = await orderService.CreateOrder(calculation.ShoppingCartData, customer, billingAddress, shippingAddress, user?.Id ?? 0, languageName,
            paymentMethodId, shippingMethodId, calculation.CalculationResult.ShippingPrice, cancellationToken);

        await currentShoppingCartDiscardHandler.Discard(cancellationToken);

        return await OrderIdToOrderNumber(orderId, cancellationToken);
    }


    private async Task<string> OrderIdToOrderNumber(int orderId, CancellationToken cancellationToken)
    {
        var orderInfo = await orderInfoProvider.GetAsync(orderId, cancellationToken);

        if (orderInfo == null)
        {
            throw new InvalidOperationException($"Order with ID {orderId} does not exist.");
        }

        return orderInfo.OrderNumber;
    }


    private async Task<bool> IsValid(CustomerAddressViewModel billingAddress, ShippingAddressViewModel shippingAddress, CancellationToken cancellationToken)
    {
        // Validate state selection based on the selected country for billing address
        int.TryParse(billingAddress.CountryId, out int billingCountryId);
        var billingCountryStates = await countryStateRepository.GetStates(billingCountryId, cancellationToken);
        bool selectedStateValidationResult = !billingCountryStates.Any() || !string.IsNullOrEmpty(billingAddress.StateId);
        if (!selectedStateValidationResult)
        {
            ModelState.AddModelError($"{nameof(billingAddress)}.{nameof(CustomerAddressViewModel.StateId)}", CheckoutFormConstants.REQUIRED_FIELD_ERROR_MESSAGE);
        }

        if (shippingAddress.IsSameAsBilling)
        {
            ModelState.Remove($"{nameof(shippingAddress)}.{nameof(shippingAddress.Line1)}");
            ModelState.Remove($"{nameof(shippingAddress)}.{nameof(shippingAddress.Line2)}");
            ModelState.Remove($"{nameof(shippingAddress)}.{nameof(shippingAddress.City)}");
            ModelState.Remove($"{nameof(shippingAddress)}.{nameof(shippingAddress.PostalCode)}");
            ModelState.Remove($"{nameof(shippingAddress)}.{nameof(shippingAddress.CountryId)}");
            ModelState.Remove($"{nameof(shippingAddress)}.{nameof(shippingAddress.StateId)}");
        }
        else
        {
            // Validate state selection based on the selected country for shipping address
            int.TryParse(shippingAddress.CountryId, out int shippingCountryId);
            var shippingCountryStates = billingCountryId == shippingCountryId ? billingCountryStates : await countryStateRepository.GetStates(shippingCountryId, cancellationToken);
            bool billingStateValidationResult = !shippingCountryStates.Any() || !string.IsNullOrEmpty(shippingAddress.StateId);
            if (!billingStateValidationResult)
            {
                ModelState.AddModelError($"{nameof(shippingAddress)}.{nameof(ShippingAddressViewModel.StateId)}", CheckoutFormConstants.REQUIRED_FIELD_ERROR_MESSAGE);
            }
        }
        return ModelState.IsValid;
    }


    /// <summary>
    /// Builds the customer step view model including the shopping cart, so the order summary card
    /// can be rendered next to the form.
    /// </summary>
    private async Task<CheckoutViewModel> GetCustomerStepViewModel(CustomerViewModel customer, CustomerAddressViewModel billingAddress, ShippingAddressViewModel shippingAddress,
        PaymentShippingViewModel paymentShipping, CancellationToken cancellationToken)
    {
        var calculation = await CalculateCurrentShoppingCart(billingAddress, paymentShipping, cancellationToken);
        if (calculation == null)
        {
            return await GetCheckoutViewModel(customer, billingAddress, shippingAddress,
                ShoppingCartViewModel.Empty, paymentShipping, 0, 0, false, cancellationToken);
        }

        return await GetCheckoutViewModel(customer, billingAddress, shippingAddress,
            calculation.ShoppingCart, paymentShipping, calculation.CalculationResult.ShippingPrice, calculation.OriginalShippingPrice, calculation.HasFreeShippingPromotion, cancellationToken);
    }


    /// <summary>
    /// Calculates the current shopping cart in checkout mode and builds its view model.
    /// Returns <see langword="null"/> when there is no current shopping cart.
    /// </summary>
    private async Task<ShoppingCartCalculation> CalculateCurrentShoppingCart(CustomerAddressViewModel billingAddress, PaymentShippingViewModel paymentShipping, CancellationToken cancellationToken)
    {
        var shoppingCart = await currentShoppingCartRetriever.Get(cancellationToken);
        if (shoppingCart == null)
        {
            return null;
        }

        var shoppingCartData = shoppingCart.GetShoppingCartDataModel();

        int.TryParse(paymentShipping?.ShippingMethodId, out var shippingMethodId);
        int.TryParse(paymentShipping?.PaymentMethodId, out var paymentMethodId);

        var calculationResult = await calculationService.Calculate(shoppingCartData, PriceCalculationMode.Checkout, shippingMethodId, paymentMethodId, billingAddress, cancellationToken);
        var (originalShippingPrice, hasFreeShippingPromotion) = await GetShippingPromotionDetails(calculationResult, shippingMethodId, cancellationToken);

        return new ShoppingCartCalculation(calculationResult, await GetShoppingCartViewModel(calculationResult, shoppingCartData, cancellationToken), shoppingCartData, originalShippingPrice, hasFreeShippingPromotion);
    }


    /// <summary>
    /// Resolves the shipping price before any shipping promotion discount, together with whether a free shipping
    /// promotion is applied.
    /// </summary>
    private async Task<(decimal OriginalShippingPrice, bool HasFreeShippingPromotion)> GetShippingPromotionDetails(DancingGoatPriceCalculationResult calculationResult, int shippingMethodId, CancellationToken cancellationToken)
    {
        var appliedPromotion = calculationResult.PromotionData.FreeShippingPromotionCandidates.FirstOrDefault(candidate => candidate.Applied);
        if (appliedPromotion == null)
        {
            return (calculationResult.ShippingPrice, false);
        }

        var shippingMethods = await shippingRepository.GetShipping(cancellationToken);
        var originalShippingPrice = shippingMethods.FirstOrDefault(method => method.ShippingMethodID == shippingMethodId)?.ShippingMethodPrice ?? calculationResult.ShippingPrice;

        return (originalShippingPrice, true);
    }


    private sealed record ShoppingCartCalculation(DancingGoatPriceCalculationResult CalculationResult, ShoppingCartViewModel ShoppingCart, ShoppingCartDataModel ShoppingCartData, decimal OriginalShippingPrice, bool HasFreeShippingPromotion);


    private async Task<CheckoutViewModel> GetCheckoutViewModel(CustomerViewModel customerViewModel, CustomerAddressViewModel billingAddressViewModel, ShippingAddressViewModel shippingAddressViewModel,
        ShoppingCartViewModel shoppingCartViewModel, PaymentShippingViewModel paymentShippingViewModel, decimal shippingPrice, decimal originalShippingPrice, bool hasFreeShippingPromotion, CancellationToken cancellationToken)
    {
        var user = await GetAuthenticatedUser();

        // No model data is provided => try to retrieve data from the registered member/customer
        if (user != null && customerViewModel == null)
        {
            // Retrieve email information for the registered member
            customerViewModel = new CustomerViewModel()
            {
                Email = user.Email,
            };

            // The registered member already has a customer account
            var customer = await customerDataRetriever.GetCustomerForMember(user.Id, cancellationToken);
            if (customer != null)
            {
                customerViewModel.FirstName = customer.CustomerFirstName;
                customerViewModel.LastName = customer.CustomerLastName;
                customerViewModel.Email = customer.CustomerEmail;
                customerViewModel.PhoneNumber = customer.CustomerPhone;

                var customerAddress = await customerDataRetriever.GetCustomerAddress(customer.CustomerID, cancellationToken);
                if (customerAddress != null)
                {
                    customerViewModel.Company = customerAddress.CustomerAddressCompany;

                    billingAddressViewModel ??= new CustomerAddressViewModel();
                    billingAddressViewModel.Line1 = customerAddress.CustomerAddressLine1;
                    billingAddressViewModel.Line2 = customerAddress.CustomerAddressLine2;
                    billingAddressViewModel.City = customerAddress.CustomerAddressCity;
                    billingAddressViewModel.PostalCode = customerAddress.CustomerAddressZip;
                    billingAddressViewModel.CountryId = customerAddress.CustomerAddressCountryID.ToString();
                    billingAddressViewModel.StateId = customerAddress.CustomerAddressStateID.ToString();
                }
            }
        }

        customerViewModel ??= new CustomerViewModel();
        billingAddressViewModel ??= new CustomerAddressViewModel();
        shippingAddressViewModel ??= new ShippingAddressViewModel();
        paymentShippingViewModel ??= new PaymentShippingViewModel();

        int.TryParse(billingAddressViewModel.CountryId, out var billingCountryId);
        int.TryParse(billingAddressViewModel.StateId, out var billingStateId);
        var countries = await countryStateRepository.GetCountries(cancellationToken);
        var billingStates = await countryStateRepository.GetStates(billingCountryId, cancellationToken);
        var countriesSelectList = countries.Select(x => new SelectListItem() { Text = x.CountryDisplayName, Value = x.CountryID.ToString() });

        billingAddressViewModel.Countries = countriesSelectList;
        billingAddressViewModel.Country = countriesSelectList.FirstOrDefault(country => country.Value == billingCountryId.ToString())?.Text;

        billingAddressViewModel.States = billingStates.Select(x => new SelectListItem() { Text = x.StateDisplayName, Value = x.StateID.ToString() }).ToList();
        billingAddressViewModel.State = billingStates.FirstOrDefault(state => state.StateID == billingStateId)?.StateDisplayName;

        int.TryParse(shippingAddressViewModel.CountryId, out var shippingCountryId);
        int.TryParse(shippingAddressViewModel.StateId, out var shippingStateId);
        var shippingStates = billingCountryId == shippingCountryId ? billingStates : await countryStateRepository.GetStates(shippingCountryId, cancellationToken);

        shippingAddressViewModel.Countries = countriesSelectList;
        shippingAddressViewModel.Country = countriesSelectList.FirstOrDefault(country => country.Value == shippingCountryId.ToString())?.Text;

        shippingAddressViewModel.States = shippingStates.Select(x => new SelectListItem() { Text = x.StateDisplayName, Value = x.StateID.ToString() }).ToList();
        shippingAddressViewModel.State = shippingStates.FirstOrDefault(state => state.StateID == shippingStateId)?.StateDisplayName;

        int.TryParse(paymentShippingViewModel.PaymentMethodId, out var selectedPaymentMethodId);

        var payment = await paymentRepository.GetPayments(cancellationToken);

        paymentShippingViewModel.Payments = payment.Select(x => new SelectListItem() { Text = x.PaymentMethodDisplayName, Value = x.PaymentMethodID.ToString() });
        paymentShippingViewModel.Shippings = await shippingMethodOptionsService.GetShippingOptions(billingAddressViewModel, selectedPaymentMethodId, cancellationToken);

        paymentShippingViewModel.ShippingPrice = shippingPrice;
        paymentShippingViewModel.OriginalShippingPrice = originalShippingPrice;
        paymentShippingViewModel.HasFreeShippingPromotion = hasFreeShippingPromotion;

        return new CheckoutViewModel(customerViewModel, billingAddressViewModel, shippingAddressViewModel, shoppingCartViewModel, paymentShippingViewModel);
    }


    private async Task<ShoppingCartViewModel> GetShoppingCartViewModel(DancingGoatPriceCalculationResult calculationResult, ShoppingCartDataModel shoppingCartData, CancellationToken cancellationToken)
    {
        var languageName = currentLanguageRetriever.Get();

        var products = await productRepository.GetProductsByIds(shoppingCartData.Items.Select(item => item.ProductIdentifier.Identifier), cancellationToken);
        var productPageUrls = await productRepository.GetProductPageUrls(products.Cast<IContentItemFieldsSource>().Select(p => p.SystemFields.ContentItemID), cancellationToken);

        var totalWithoutShippingAndTax = priceCalculationTotalsCalculator.GetTotalWithoutShippingAndTax(calculationResult);
        var linesSubtotal = priceCalculationTotalsCalculator.GetLinesSubtotal(calculationResult);
        var totalDiscount = priceCalculationTotalsCalculator.GetTotalDiscountAmount(calculationResult);

        return new ShoppingCartViewModel(
            shoppingCartData.Items.Select(item =>
            {
                var product = products.FirstOrDefault(product => (product as IContentItemFieldsSource)?.SystemFields.ContentItemID == item.ProductIdentifier.Identifier);
                productPageUrls.TryGetValue(item.ProductIdentifier.Identifier, out var pageUrl);
                var productName = productNameProvider.GetProductName(product, item.ProductIdentifier.VariantIdentifier);

                var calculationItem = calculationResult.Items.FirstOrDefault(i => i.ProductIdentifier == item.ProductIdentifier);

                return ((product == null) || (calculationItem == null))
                    ? null
                    : new ShoppingCartItemViewModel(
                        item.ProductIdentifier.Identifier,
                        productName,
                        product.ProductFieldImage.FirstOrDefault()?.ImageFile.Url,
                        pageUrl,
                        item.Quantity,
                        calculationItem.LineSubtotalAfterLineDiscount,
                        item.Quantity * product.ProductFieldPrice,
                        item.ProductIdentifier.VariantIdentifier);
            })
            .Where(x => x != null)
            .ToList(),
            calculationResult.GrandTotal,
            linesSubtotal,
            totalWithoutShippingAndTax,
            calculationResult.TotalTax,
            totalDiscount,
            null,
            Enumerable.Empty<CouponCodeViewModel>());
    }


    /// <summary>
    /// Retrieves an authenticated live site user.
    /// </summary>
    /// <seealso cref="MemberInfo"/>"/>
    private async Task<ApplicationUser> GetAuthenticatedUser() => await userManager.GetUserAsync(User);
}
