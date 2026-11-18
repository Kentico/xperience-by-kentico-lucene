using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CMS.Commerce;

using DancingGoat.Models;

using Kentico.Xperience.Admin.DigitalCommerce;

using Microsoft.Extensions.Localization;

namespace DancingGoat.Commerce;

/// <summary>
/// Tells which products earn an active free shipping promotion on their own, in quantity one, and builds the
/// free shipping messages the product detail advertises.
/// </summary>
/// <remarks>
/// The service evaluates every active free shipping promotion that has no coupon and uses the
/// <see cref="DancingGoatFreeShippingPromotionRule"/>. Promotions with a different promotion rule are not evaluated.
/// </remarks>
public sealed class FreeShippingEligibilityService
{
    private readonly PromotionRepository promotionRepository;
    private readonly ShippingRepository shippingRepository;

    private readonly IPromotionCustomerEligibilityValidator promotionCustomerEligibilityValidator;
    private readonly BuyerIdentifierRetriever buyerIdentifierRetriever;
    private readonly IPriceFormatter priceFormatter;
    private readonly IStringLocalizer<SharedResources> localizer;


    /// <summary>
    /// Creates a new instance of <see cref="FreeShippingEligibilityService"/>.
    /// </summary>
    public FreeShippingEligibilityService(
        IPromotionCustomerEligibilityValidator promotionCustomerEligibilityValidator,
        BuyerIdentifierRetriever buyerIdentifierRetriever,
        IPriceFormatter priceFormatter,
        IStringLocalizer<SharedResources> localizer,
        PromotionRepository promotionRepository,
        ShippingRepository shippingRepository)
    {
        this.promotionCustomerEligibilityValidator = promotionCustomerEligibilityValidator;
        this.buyerIdentifierRetriever = buyerIdentifierRetriever;
        this.priceFormatter = priceFormatter;
        this.localizer = localizer;
        this.promotionRepository = promotionRepository;
        this.shippingRepository = shippingRepository;
    }


    /// <summary>
    /// Returns content item identifiers of products whose catalog price calculation item alone meets an active
    /// free shipping promotion.
    /// </summary>
    /// <param name="calculationResultItems">Catalog price calculation items of the listed products, one per product with quantity one.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    public async Task<ISet<int>> GetFreeShippingEligibleProductIds(IEnumerable<DancingGoatPriceCalculationResultItem> calculationResultItems, CancellationToken cancellationToken)
    {
        var promotionProperties = await GetActivePromotionProperties(cancellationToken);
        var eligibleProductIds = new HashSet<int>();

        if (promotionProperties.Count == 0)
        {
            return eligibleProductIds;
        }

        foreach (var item in calculationResultItems)
        {
            if (promotionProperties.Any(properties => FreeShippingThresholdEvaluator.QualifiesAlone(properties, item)))
            {
                eligibleProductIds.Add(item.ProductIdentifier.Identifier);
            }
        }

        return eligibleProductIds;
    }


    /// <summary>
    /// Gets the active free shipping promotions the product counts toward, each with a message telling the
    /// customer how to earn the free shipping with the product.
    /// </summary>
    /// <param name="calculationResultItem">Catalog calculation item of the product.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>
    /// One view model per advertised promotion. A promotion the product can never earn - a single item price
    /// requirement that the product's unit price does not reach - is left out, so that the product detail makes
    /// no promise the customer cannot keep.
    /// </returns>
    /// <remarks>
    /// The messages are built from the promotion configuration and from the catalog price the product detail
    /// has already calculated, so no additional price calculation runs. 
    /// Only the promotions implementing the <see cref="DancingGoatFreeShippingPromotionRule"/> are loaded.
    /// </remarks>
    public async Task<IReadOnlyList<ProductFreeShippingPromotionViewModel>> GetFreeShippingPromotions(DancingGoatPriceCalculationResultItem calculationResultItem, CancellationToken cancellationToken)
    {
        var promotions = await promotionRepository.GetAvailablePromotions(PromotionType.Shipping, DancingGoatFreeShippingPromotionRule.IDENTIFIER, cancellationToken);
        if (promotions.Count() == 0)
        {
            return [];
        }

        var activatingCouponCodes = await promotionRepository.GetActivatingCouponCodes(promotions.Select(promotion => promotion.PromotionID), cancellationToken);
        var buyerIdentifier = await buyerIdentifierRetriever.GetCurrentBuyerIdentifier();
        decimal unitPrice = GetUnitPriceAfterAllDiscounts(calculationResultItem);

        var advertisedPromotions = new List<ProductFreeShippingPromotionViewModel>();

        foreach (var promotion in promotions)
        {
            var properties = promotion.GetPromotionRuleProperties<DancingGoatFreeShippingProperties>();
            if (properties == null || !DancingGoatFreeShippingPromotionRuleUtils.IsInScope(properties, calculationResultItem.ProductData))
            {
                continue;
            }

            string message = GetPromotionMessage(properties, unitPrice);
            if (message == null)
            {
                continue;
            }

            var coveredShippingMethodNames = await GetCoveredShippingMethodNames(properties, cancellationToken);
            if (coveredShippingMethodNames is not null)
            {
                // The reward covers no shipping method the customer can pick, so the free shipping is out of reach.
                if (coveredShippingMethodNames.Count == 0)
                {
                    continue;
                }

                string shippingMethods = PromotionMessageFormatter.JoinWithOr(localizer, coveredShippingMethodNames);
                message = $"{message} {string.Format(localizer["Applies to {0} only."], shippingMethods)}";
            }

            if (activatingCouponCodes.TryGetValue(promotion.PromotionID, out string couponCode))
            {
                message = $"{message} {string.Format(localizer["Use the coupon code '{0}' in shopping cart."], couponCode)}";
            }

            if (await RequiresSignIn(promotion, buyerIdentifier, cancellationToken))
            {
                message = $"{message} {localizer["Available only to registered customers - sign in to get it."]}";
            }

            advertisedPromotions.Add(new ProductFreeShippingPromotionViewModel(message));
        }

        return advertisedPromotions;
    }


    /// <summary>
    /// Names the shipping methods the free shipping reward covers.
    /// </summary>
    /// <param name="properties">Properties of the promotion rule.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>
    /// Display names of the covered shipping methods, or <c>null</c> when the reward covers every shipping method
    /// and the message therefore needs no restriction. An empty result means that no shipping method the customer
    /// can pick is covered any more, because every selected one has been disabled or deleted.
    /// </returns>
    private async Task<IReadOnlyList<string>> GetCoveredShippingMethodNames(FreeShippingPromotionRuleProperties properties, CancellationToken cancellationToken)
    {
        if (!string.Equals(properties.ShippingMethodScope, FreeShippingPromotionRuleProperties.SHIPPING_METHODS_SPECIFIC, StringComparison.InvariantCultureIgnoreCase))
        {
            return null;
        }

        var coveredGuids = properties.ShippingMethods.Select(shippingMethod => shippingMethod.ObjectGuid).ToHashSet();
        var shippingMethods = await shippingRepository.GetShipping(cancellationToken);

        return [.. shippingMethods
            .Where(shippingMethod => coveredGuids.Contains(shippingMethod.ShippingMethodGUID))
            .Select(shippingMethod => shippingMethod.ShippingMethodDisplayName)];
    }


    /// <summary>
    /// Tells whether the promotion is reserved for registered customers and the current visitor is not signed in.
    /// </summary>
    /// <param name="promotion">Promotion to examine.</param>
    /// <param name="buyerIdentifier">Identifier of the current buyer.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    private async Task<bool> RequiresSignIn(PromotionInfo promotion, BuyerIdentifier buyerIdentifier, CancellationToken cancellationToken)
    {
        if (!promotion.PromotionCustomerEligibility.Equals(PromotionCustomerEligibility.Registered))
        {
            return false;
        }

        var eligibilityResult = await promotionCustomerEligibilityValidator.Validate(promotion.PromotionCustomerEligibility, buyerIdentifier, cancellationToken);

        return !eligibilityResult.IsEligible;
    }


    /// <summary>
    /// Builds the message describing what the customer buys to earn the free shipping of the promotion.
    /// </summary>
    /// <param name="properties">Properties of the promotion rule.</param>
    /// <param name="unitPrice">Unit price of the product after all discounts.</param>
    /// <returns>The message, or <c>null</c> when the product can never earn the promotion.</returns>
    private string GetPromotionMessage(DancingGoatFreeShippingProperties properties, decimal unitPrice)
    {
        decimal requiredValue = properties.MinimumRequirementValue;
        string alreadyQualifiesMessage = localizer["You get free shipping with this product."];

        switch (properties.MinimumRequirementValueType)
        {
            case FreeShippingMinimumRequirementType.None:
                return alreadyQualifiesMessage;

            case FreeShippingMinimumRequirementType.Quantity:
                if (requiredValue <= 1)
                {
                    return alreadyQualifiesMessage;
                }

                string quantityMessageResource = localizer["Buy {0} or more items and get free shipping."];
                return string.Format(quantityMessageResource, (int)Math.Ceiling(requiredValue));

            case FreeShippingMinimumRequirementType.Price:
                if (unitPrice >= requiredValue)
                {
                    return alreadyQualifiesMessage;
                }

                string priceMessageResource = localizer["Spend {0} or more and get free shipping."];
                return string.Format(priceMessageResource, priceFormatter.Format(requiredValue, new PriceFormatContext()));

            case FreeShippingMinimumRequirementType.ItemPrice:
                return unitPrice >= requiredValue ? alreadyQualifiesMessage : null;

            default:
                return null;
        }
    }


    private async Task<IReadOnlyList<DancingGoatFreeShippingProperties>> GetActivePromotionProperties(CancellationToken cancellationToken)
    {
        var promotions = await promotionRepository.GetActivePromotions(PromotionType.Shipping, [], cancellationToken);

        return promotions
            .Where(IsSampleRule)
            .Select(promotion => promotion.GetPromotionRuleProperties<DancingGoatFreeShippingProperties>())
            .ToList();
    }


    private static bool IsSampleRule(PromotionInfo promotion) =>
        string.Equals(promotion.PromotionRuleIdentifier, DancingGoatFreeShippingPromotionRule.IDENTIFIER, StringComparison.InvariantCultureIgnoreCase);


    private static decimal GetUnitPriceAfterAllDiscounts(DancingGoatPriceCalculationResultItem item) =>
        item.Quantity > 0 ? item.LineSubtotalAfterAllDiscounts / item.Quantity : 0;
}
