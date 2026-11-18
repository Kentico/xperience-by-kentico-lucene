using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CMS.Commerce;

using DancingGoat.Helpers;
using DancingGoat.Models;

using Kentico.Commerce.Web.Mvc;

using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;

namespace DancingGoat.Commerce;

/// <summary>
/// Builds the options of the checkout shipping method selector, each labeled with the price the customer
/// actually pays for that method.
/// </summary>
/// <remarks>
/// The calculated price is not the price configured on the shipping method: a free shipping promotion can cover
/// some methods and leave the others untouched. The price therefore comes from a checkout calculation run once per
/// shipping method with the current cart, its coupon codes, and the billing address filled in so far.
/// </remarks>
public sealed class ShippingMethodOptionsService
{
    private readonly ICurrentShoppingCartRetriever currentShoppingCartRetriever;
    private readonly ShippingRepository shippingRepository;
    private readonly CalculationService calculationService;
    private readonly IPriceFormatter priceFormatter;
    private readonly IStringLocalizer<SharedResources> localizer;


    /// <summary>
    /// Creates a new instance of <see cref="ShippingMethodOptionsService"/>.
    /// </summary>
    public ShippingMethodOptionsService(
        ICurrentShoppingCartRetriever currentShoppingCartRetriever,
        ShippingRepository shippingRepository,
        CalculationService calculationService,
        IPriceFormatter priceFormatter,
        IStringLocalizer<SharedResources> localizer)
    {
        this.currentShoppingCartRetriever = currentShoppingCartRetriever;
        this.shippingRepository = shippingRepository;
        this.calculationService = calculationService;
        this.priceFormatter = priceFormatter;
        this.localizer = localizer;
    }


    /// <summary>
    /// Returns the shipping method selector options, labeled with the calculated price of each method.
    /// </summary>
    /// <param name="billingAddress">Billing address filled in on the checkout form, or <c>null</c> when not filled in yet.</param>
    /// <param name="paymentMethodId">Identifier of the selected payment method, or zero when none is selected.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    public async Task<IReadOnlyList<SelectListItem>> GetShippingOptions(CustomerAddressViewModel billingAddress, int paymentMethodId, CancellationToken cancellationToken)
    {
        var shippingMethods = await shippingRepository.GetShipping(cancellationToken);

        var shoppingCart = await currentShoppingCartRetriever.Get(cancellationToken);
        if (shoppingCart == null)
        {
            return [.. shippingMethods.Select(method => GetOption(method, method.ShippingMethodPrice))];
        }

        var shoppingCartData = shoppingCart.GetShoppingCartDataModel();
        var options = new List<SelectListItem>();

        foreach (var method in shippingMethods)
        {
            var calculationResult = await calculationService.Calculate(shoppingCartData, PriceCalculationMode.Checkout, method.ShippingMethodID, paymentMethodId, billingAddress, cancellationToken);

            options.Add(GetOption(method, calculationResult.ShippingPrice));
        }

        return options;
    }


    private SelectListItem GetOption(ShippingMethodInfo shippingMethod, decimal calculatedPrice)
    {
        return new SelectListItem()
        {
            Text = string.Format(localizer["{0} — {1}"], shippingMethod.ShippingMethodDisplayName, priceFormatter.Format(calculatedPrice, new PriceFormatContext())),
            Value = shippingMethod.ShippingMethodID.ToString()
        };
    }
}
