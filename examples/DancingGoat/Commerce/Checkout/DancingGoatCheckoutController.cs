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
    private readonly WebPageUrlProvider webPageUrlProvider;
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
    private readonly IInfoProvider<OrderInfo> orderInfoProvider;

    public DancingGoatCheckoutController(
        CountryStateRepository countryStateRepository,
        WebPageUrlProvider webPageUrlProvider,
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
        IInfoProvider<OrderInfo> orderInfoProvider)
    {
        this.countryStateRepository = countryStateRepository;
        this.webPageUrlProvider = webPageUrlProvider;
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
        this.orderInfoProvider = orderInfoProvider;
    }


    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(await GetCheckoutViewModel(CheckoutStep.CheckoutCustomer, null, null, null, null, null, 0, cancellationToken));
    }


    [HttpPost]
    public async Task<IActionResult> Index(CustomerViewModel customer, CustomerAddressViewModel billingAddress, ShippingAddressViewModel shippingAddress, PaymentShippingViewModel paymentShipping, CheckoutStep checkoutStep, CancellationToken cancellationToken)
    {
        if (!await IsValid(billingAddress, shippingAddress, cancellationToken) || checkoutStep == CheckoutStep.CheckoutCustomer)
        {
            return View(await GetCheckoutViewModel(CheckoutStep.CheckoutCustomer, customer, billingAddress, shippingAddress, null, paymentShipping, 0, cancellationToken));
        }

        var shoppingCart = await currentShoppingCartRetriever.Get(cancellationToken);
        if (shoppingCart == null)
        {
            return View(await GetCheckoutViewModel(CheckoutStep.OrderConfirmation, customer, billingAddress, shippingAddress,
                new ShoppingCartViewModel(new List<ShoppingCartItemViewModel>(), 0, 0, 0, 0), paymentShipping, 0, cancellationToken));
        }

        var shoppingCartData = shoppingCart.GetShoppingCartDataModel();

        int.TryParse(paymentShipping.ShippingMethodId, out var shippingMethodId);
        int.TryParse(paymentShipping.PaymentMethodId, out var paymentMethodId);

        var calculationResult = await calculationService.Calculate(shoppingCartData, PriceCalculationMode.Checkout, shippingMethodId, paymentMethodId, billingAddress, cancellationToken);

        var checkoutViewModel = await GetCheckoutViewModel(CheckoutStep.OrderConfirmation, customer, billingAddress, shippingAddress, null, paymentShipping, calculationResult.ShippingPrice, cancellationToken);
        checkoutViewModel = checkoutViewModel with
        {
            ShoppingCart = await GetShoppingCartViewModel(calculationResult, shoppingCartData, cancellationToken)
        };

        return View(checkoutViewModel);
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


    [HttpPost]
    [Route("{languageName}/OrderConfirmation/ConfirmOrder")]
    public async Task<IActionResult> ConfirmOrder(CustomerViewModel customer, CustomerAddressViewModel billingAddress, ShippingAddressViewModel shippingAddress, string languageName, PaymentShippingViewModel paymentShipping, CancellationToken cancellationToken)
    {
        // Add the current language to the route values in order to tell XbyK what the current language is
        // since this route is not handled by the XbyK content-tree-based routing
        HttpContext.Request.RouteValues.Add(WebPageRoutingOptions.LANGUAGE_ROUTE_VALUE_KEY, languageName);

        if (!ModelState.IsValid)
        {
            Redirect(await webPageUrlProvider.CheckoutPageUrl(languageName, cancellationToken: cancellationToken));
        }

        var user = await GetAuthenticatedUser();

        var shoppingCart = await currentShoppingCartRetriever.Get(cancellationToken);
        if (shoppingCart == null)
        {
            return Content(localizer["Order not created. The shopping cart could not be found."]);
        }

        var shoppingCartData = shoppingCart.GetShoppingCartDataModel();

        int.TryParse(paymentShipping.PaymentMethodId, out var paymentMethodId);
        int.TryParse(paymentShipping.ShippingMethodId, out var shippingMethodId);

        var orderId = await orderService.CreateOrder(shoppingCartData, customer, billingAddress, shippingAddress, user?.Id ?? 0, languageName, paymentMethodId, shippingMethodId, paymentShipping.ShippingPrice, cancellationToken);

        await currentShoppingCartDiscardHandler.Discard(cancellationToken);

        var orderNumber = await OrderIdToOrderNumber(orderId, cancellationToken);

        return View(new ConfirmOrderViewModel(orderNumber));
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


    private async Task<CheckoutViewModel> GetCheckoutViewModel(CheckoutStep step, CustomerViewModel customerViewModel, CustomerAddressViewModel billingAddressViewModel, ShippingAddressViewModel shippingAddressViewModel,
        ShoppingCartViewModel shoppingCartViewModel, PaymentShippingViewModel paymentShippingViewModel, decimal shippingPrice, CancellationToken cancellationToken)
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

        var payment = await paymentRepository.GetPayments(cancellationToken);
        var shipping = await shippingRepository.GetShipping(cancellationToken);

        paymentShippingViewModel.Payments = payment.Select(x => new SelectListItem() { Text = x.PaymentMethodDisplayName, Value = x.PaymentMethodID.ToString() });
        paymentShippingViewModel.Shippings = shipping.Select(x => new SelectListItem() { Text = x.ShippingMethodDisplayName, Value = x.ShippingMethodID.ToString() });

        paymentShippingViewModel.ShippingPrice = shippingPrice;

        return new CheckoutViewModel(step, customerViewModel, billingAddressViewModel, shippingAddressViewModel, shoppingCartViewModel, paymentShippingViewModel);
    }


    private async Task<ShoppingCartViewModel> GetShoppingCartViewModel(DancingGoatPriceCalculationResult calculationResult, ShoppingCartDataModel shoppingCartData, CancellationToken cancellationToken)
    {
        var languageName = currentLanguageRetriever.Get();

        var products = await productRepository.GetProductsByIds(shoppingCartData.Items.Select(item => item.ProductIdentifier.Identifier), cancellationToken);

        var productPageUrls = await productRepository.GetProductPageUrls(products.Cast<IContentItemFieldsSource>().Select(p => p.SystemFields.ContentItemID), cancellationToken);

        var subtotal = calculationResult.Items.Sum(x => x.LineSubtotalAfterLineDiscount);

        var orderDiscount = calculationResult.PromotionData.OrderPromotionCandidates.FirstOrDefault()?.PromotionCandidate.OrderDiscountAmount ?? 0;

        return new ShoppingCartViewModel(
            shoppingCartData.Items.Select(item =>
            {
                var product = products.FirstOrDefault(product => (product as IContentItemFieldsSource)?.SystemFields.ContentItemID == item.ProductIdentifier.Identifier);
                productPageUrls.TryGetValue(item.ProductIdentifier.Identifier, out var pageUrl);
                var productName = productNameProvider.GetProductName(product, item.ProductIdentifier.VariantIdentifier);

                var calculationItem = calculationResult.Items.FirstOrDefault(i => i.ProductIdentifier == item.ProductIdentifier);

                return product == null
                    ? null
                    : new ShoppingCartItemViewModel(
                        item.ProductIdentifier.Identifier,
                        productName,
                        product.ProductFieldImage.FirstOrDefault()?.ImageFile.Url,
                        pageUrl,
                        item.Quantity,
                        product.ProductFieldPrice,
                        calculationItem?.LineSubtotalAfterLineDiscount ?? item.Quantity * product.ProductFieldPrice,
                        item.Quantity * product.ProductFieldPrice,
                        calculationItem?.PromotionData.CatalogPromotionCandidates.FirstOrDefault(c => c.Applied)?.PromotionCandidate as DancingGoatCatalogPromotionCandidate,
                        item.ProductIdentifier.VariantIdentifier);
            })
            .Where(x => x != null)
            .ToList(),
            calculationResult.GrandTotal,
            subtotal,
            calculationResult.TotalTax,
            orderDiscount);
    }


    /// <summary>
    /// Retrieves an authenticated live site user.
    /// </summary>
    /// <seealso cref="MemberInfo"/>"/>
    private async Task<ApplicationUser> GetAuthenticatedUser() => await userManager.GetUserAsync(User);
}
