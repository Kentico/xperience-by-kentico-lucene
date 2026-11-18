using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CMS.Commerce;
using CMS.ContentEngine;

using DancingGoat.Models;

using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;

namespace DancingGoat.Commerce;

public sealed class CalculationService
{
    private readonly BuyerIdentifierRetriever buyerIdentifierRetriever;
    private readonly IPriceCalculationService<DancingGoatPriceCalculationRequest, DancingGoatPriceCalculationResult> priceCalculationService;
    private readonly IPreferredLanguageRetriever preferredLanguageRetriever;


    public CalculationService(BuyerIdentifierRetriever buyerIdentifierRetriever, IPriceCalculationService<DancingGoatPriceCalculationRequest, DancingGoatPriceCalculationResult> priceCalculationService, IWebPageDataContextRetriever webPageDataContextRetriever, IPreferredLanguageRetriever preferredLanguageRetriever)
    {
        this.buyerIdentifierRetriever = buyerIdentifierRetriever;
        this.priceCalculationService = priceCalculationService;
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
        var buyerIdentifier = await buyerIdentifierRetriever.GetCurrentBuyerIdentifier();

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
            BuyerIdentifier = buyerIdentifier,
            LanguageName = preferredLanguageRetriever.Get(),
            Mode = mode,
            CouponCodes = shoppingCartData.CouponCodes
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
