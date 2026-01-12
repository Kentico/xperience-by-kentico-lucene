using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CMS.Commerce;
using CMS.ContentEngine;

using DancingGoat.Models;

using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.Membership;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace DancingGoat.Commerce;

public sealed class CalculationService
{
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly IPriceCalculationService<DancingGoatPriceCalculationRequest, DancingGoatPriceCalculationResult> priceCalculationService;
    private readonly CustomerDataRetriever customerDataRetriever;
    private readonly IPreferredLanguageRetriever preferredLanguageRetriever;


    public CalculationService(IHttpContextAccessor httpContextAccessor, IPriceCalculationService<DancingGoatPriceCalculationRequest, DancingGoatPriceCalculationResult> priceCalculationService, CustomerDataRetriever customerDataRetriever, IWebPageDataContextRetriever webPageDataContextRetriever, IPreferredLanguageRetriever preferredLanguageRetriever)
    {
        this.httpContextAccessor = httpContextAccessor;
        this.priceCalculationService = priceCalculationService;
        this.customerDataRetriever = customerDataRetriever;
        this.preferredLanguageRetriever = preferredLanguageRetriever;
    }


    /// <summary>
    /// Calculate total price of all items within shopping cart with specified products with no shipping specified.
    /// </summary>
    public async Task<DancingGoatPriceCalculationResult> CalculateShoppingCart(ShoppingCartDataModel shoppingCartData, CancellationToken cancellationToken)
    {
        return await Calculate(shoppingCartData, PriceCalculationMode.ShoppingCart, 0, 0, null, cancellationToken);
    }


    /// <summary>
    /// Calculate total price of all items within shopping cart with specified products with no shipping specified.
    /// </summary>
    public async Task<DancingGoatPriceCalculationResult> CalculateCatalogPrices(ShoppingCartDataModel shoppingCartData, CancellationToken cancellationToken)
    {
        return await Calculate(shoppingCartData, PriceCalculationMode.Catalog, 0, 0, null, cancellationToken);
    }


    /// <summary>
    /// Calculate catalog prices for specified products.
    /// </summary>
    /// <param name="products">Products for prices calculation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<ICollection<DancingGoatPriceCalculationResultItem>> CalculateCatalogPrices(IEnumerable<IProductFields> products, CancellationToken cancellationToken)
    {
        var cartModel = new ShoppingCartDataModel()
        {
            Items = products.Select(p => new ShoppingCartDataItem
            {
                ProductIdentifier = new ProductVariantIdentifier() { Identifier = (p as IContentItemFieldsSource).SystemFields.ContentItemID },
                Quantity = 1
            }).ToList()
        };

        return (await CalculateCatalogPrices(cartModel, cancellationToken)).Items;
    }


    /// <summary>
    /// Calculate total price of all items within shopping cart with specified products.
    /// </summary>
    public async Task<DancingGoatPriceCalculationResult> Calculate(ShoppingCartDataModel shoppingCartData, PriceCalculationMode mode, int shippingMethodId, int paymentMethodId, CustomerAddressViewModel customerAddress, CancellationToken cancellationToken)
    {
        var userManager = httpContextAccessor.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();

        var user = await userManager.GetUserAsync(httpContextAccessor.HttpContext?.User);
        int customerId = 0;

        if (user != null)
        {
            // The registered member already has a customer account
            var customer = await customerDataRetriever.GetCustomerForMember(user.Id, cancellationToken);
            if (customer != null)
            {
                customerId = customer.CustomerID;
            }
        }

        var calculationRequest = new DancingGoatPriceCalculationRequest
        {
            Items = [.. shoppingCartData.Items.Select(item => new DancingGoatPriceCalculationRequestItem
            {
                ProductIdentifier = item.ProductIdentifier,
                Quantity = item.Quantity,
            })],
            ShippingMethodId = shippingMethodId,
            PaymentMethodId = paymentMethodId,
            BillingAddress = GetCustomerData(customerAddress),
            CustomerId = customerId,
            LanguageName = preferredLanguageRetriever.Get(),
            Mode = mode
        };

        return await priceCalculationService.Calculate(calculationRequest, cancellationToken);
    }


    private static AddressDto GetCustomerData(CustomerAddressViewModel customerAddress)
    {
        return customerAddress == null ? null : new AddressDto()
        {
            Line1 = customerAddress.Line1,
            Line2 = customerAddress.Line2,
            City = customerAddress.City,
            Zip = customerAddress.PostalCode,
            CountryID = int.TryParse(customerAddress.CountryId, out var countryId) ? countryId : 0,
            StateID = int.TryParse(customerAddress.StateId, out var stateId) ? stateId : 0,
        };
    }
}
