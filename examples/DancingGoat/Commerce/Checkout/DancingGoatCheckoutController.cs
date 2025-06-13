using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CMS.Commerce;
using CMS.ContentEngine;
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

#pragma warning disable KXE0002 // Commerce feature is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
[assembly: RegisterWebPageRoute(Checkout.CONTENT_TYPE_NAME, typeof(DancingGoatCheckoutController), WebsiteChannelNames = new[] { DancingGoatConstants.WEBSITE_CHANNEL_NAME })]

namespace DancingGoat.Commerce;

/// <summary>
/// Controller for managing the checkout process.
/// </summary>
public sealed class DancingGoatCheckoutController : Controller
{
    private readonly CountryStateRepository countryStateRepository;
    private readonly WebPageUrlProvider webPageUrlProvider;
    private readonly ICurrentShoppingCartService currentShoppingCartService;
    private readonly UserManager<ApplicationUser> userManager;
    private readonly CustomerDataRetriever customerDataRetriever;
    private readonly IPreferredLanguageRetriever currentLanguageRetriever;
    private readonly OrderService orderService;
    private readonly IStringLocalizer<SharedResources> localizer;
    private readonly ProductNameProvider productNameProvider;
    private readonly ProductRepository productRepository;

    public DancingGoatCheckoutController(
        CountryStateRepository countryStateRepository,
        WebPageUrlProvider webPageUrlProvider,
        ICurrentShoppingCartService currentShoppingCartService,
        UserManager<ApplicationUser> userManager,
        CustomerDataRetriever customerDataRetriever,
        IPreferredLanguageRetriever currentLanguageRetriever,
        OrderService orderService,
        IStringLocalizer<SharedResources> localizer,
        ProductNameProvider productNameProvider,
        ProductRepository productRepository)
    {
        this.countryStateRepository = countryStateRepository;
        this.webPageUrlProvider = webPageUrlProvider;
        this.currentShoppingCartService = currentShoppingCartService;
        this.userManager = userManager;
        this.customerDataRetriever = customerDataRetriever;
        this.currentLanguageRetriever = currentLanguageRetriever;
        this.orderService = orderService;
        this.localizer = localizer;
        this.productNameProvider = productNameProvider;
        this.productRepository = productRepository;
    }


    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(await GetCheckoutViewModel(CheckoutStep.CheckoutCustomer, null, null, null, cancellationToken));
    }


    [HttpPost]
    public async Task<IActionResult> Index(CustomerViewModel customer, CustomerAddressViewModel customerAddress, CheckoutStep checkoutStep, CancellationToken cancellationToken)
    {
        // Validate state selection based on the selected country
        int.TryParse(customerAddress.CountryId, out int countryId);
        var countryStates = await countryStateRepository.GetStates(countryId, cancellationToken);
        bool selectedStateValidationResult = !countryStates.Any() || !string.IsNullOrEmpty(customerAddress.StateId);
        if (!selectedStateValidationResult)
        {
            ModelState.AddModelError($"{nameof(customerAddress)}.{nameof(CustomerAddressViewModel.StateId)}", CheckoutFormConstants.REQUIRED_FIELD_ERROR_MESSAGE);
        }

        if (!ModelState.IsValid || checkoutStep == CheckoutStep.CheckoutCustomer)
        {
            return View(await GetCheckoutViewModel(CheckoutStep.CheckoutCustomer, customer, customerAddress, null, cancellationToken));
        }

        var shoppingCart = await currentShoppingCartService.Get(cancellationToken);
        if (shoppingCart == null)
        {
            return View(await GetCheckoutViewModel(CheckoutStep.OrderConfirmation, customer, customerAddress, new ShoppingCartViewModel(new List<ShoppingCartItemViewModel>(), 0), cancellationToken));
        }

        var shoppingCartViewModel = await GetShoppingCartViewModel(shoppingCart, cancellationToken);

        return View(await GetCheckoutViewModel(CheckoutStep.OrderConfirmation, customer, customerAddress, shoppingCartViewModel, cancellationToken));
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
    public async Task<IActionResult> ConfirmOrder(CustomerViewModel customer, CustomerAddressViewModel customerAddress, string languageName, CancellationToken cancellationToken)
    {
        // Add the current language to the route values in order to tell XbyK what the current language is
        // since this route is not handled by the XbyK content-tree-based routing
        HttpContext.Request.RouteValues.Add(WebPageRoutingOptions.LANGUAGE_ROUTE_VALUE_KEY, languageName);

        if (!ModelState.IsValid)
        {
            Redirect(await webPageUrlProvider.CheckoutPageUrl(languageName, cancellationToken: cancellationToken));
        }

        var user = await GetAuthenticatedUser();

        var shoppingCart = await currentShoppingCartService.Get(cancellationToken);
        if (shoppingCart == null)
        {
            return Content(localizer["Order not created. The shopping cart could not be found."]);
        }

        var customerDto = customer.ToCustomerDto(customerAddress);
        var shoppingCartData = shoppingCart.GetShoppingCartDataModel();

        var orderNumber = await orderService.CreateOrder(shoppingCartData, customerDto, user?.Id ?? 0, cancellationToken);

        await currentShoppingCartService.Discard(cancellationToken);

        return View(new ConfirmOrderViewModel(orderNumber));
    }


    private async Task<CheckoutViewModel> GetCheckoutViewModel(CheckoutStep step, CustomerViewModel customerViewModel, CustomerAddressViewModel customerAddressViewModel, ShoppingCartViewModel shoppingCartViewModel,
        CancellationToken cancellationToken)
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

                    customerAddressViewModel ??= new CustomerAddressViewModel();
                    customerAddressViewModel.Line1 = customerAddress.CustomerAddressLine1;
                    customerAddressViewModel.Line2 = customerAddress.CustomerAddressLine2;
                    customerAddressViewModel.City = customerAddress.CustomerAddressCity;
                    customerAddressViewModel.PostalCode = customerAddress.CustomerAddressZip;
                    customerAddressViewModel.CountryId = customerAddress.CustomerAddressCountryID.ToString();
                    customerAddressViewModel.StateId = customerAddress.CustomerAddressStateID.ToString();
                }
            }
        }

        customerViewModel ??= new CustomerViewModel();
        customerAddressViewModel ??= new CustomerAddressViewModel();

        int.TryParse(customerAddressViewModel.CountryId, out var countryId);
        int.TryParse(customerAddressViewModel.StateId, out var stateId);
        var countries = await countryStateRepository.GetCountries(cancellationToken);
        var states = await countryStateRepository.GetStates(countryId, cancellationToken);
        var countriesSelectList = countries.Select(x => new SelectListItem() { Text = x.CountryDisplayName, Value = x.CountryID.ToString() });

        customerAddressViewModel.Countries = countriesSelectList;
        customerAddressViewModel.Country = countriesSelectList.FirstOrDefault(country => country.Value == countryId.ToString())?.Text;

        customerAddressViewModel.States = states.Select(x => new SelectListItem() { Text = x.StateDisplayName, Value = x.StateID.ToString() }).ToList();
        customerAddressViewModel.State = states.FirstOrDefault(state => state.StateID == stateId)?.StateDisplayName;

        return new CheckoutViewModel(step, customerViewModel, customerAddressViewModel, shoppingCartViewModel);
    }


    private async Task<ShoppingCartViewModel> GetShoppingCartViewModel(ShoppingCartInfo shoppingCart, CancellationToken cancellationToken)
    {
        var languageName = currentLanguageRetriever.Get();
        var shoppingCartData = shoppingCart.GetShoppingCartDataModel();

        var products = await productRepository.GetProductsByIds(shoppingCartData.Items.Select(item => item.ContentItemId), cancellationToken);

        var productPageUrls = await productRepository.GetProductPageUrls(products.Cast<IContentItemFieldsSource>().Select(p => p.SystemFields.ContentItemID), cancellationToken);

        var totalPrice = CalculationService.CalculateTotalPrice(shoppingCartData, products);

        return new ShoppingCartViewModel(
            shoppingCartData.Items.Select(item =>
            {
                var product = products.FirstOrDefault(product => (product as IContentItemFieldsSource)?.SystemFields.ContentItemID == item.ContentItemId);
                productPageUrls.TryGetValue(item.ContentItemId, out var pageUrl);
                var productName = productNameProvider.GetProductName(product, item.VariantId);

                return product == null
                    ? null
                    : new ShoppingCartItemViewModel(
                        item.ContentItemId,
                        productName,
                        product.ProductFieldImage.FirstOrDefault()?.ImageFile.Url,
                        pageUrl,
                        item.Quantity,
                        product.ProductFieldPrice,
                        item.Quantity * product.ProductFieldPrice,
                        item.VariantId);
            })
            .Where(x => x != null)
            .ToList(),
            totalPrice);
    }


    /// <summary>
    /// Retrieves an authenticated live site user.
    /// </summary>
    /// <seealso cref="MemberInfo"/>"/>
    private async Task<ApplicationUser> GetAuthenticatedUser() => await userManager.GetUserAsync(User);
}

#pragma warning restore KXE0002 // Commerce feature is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
