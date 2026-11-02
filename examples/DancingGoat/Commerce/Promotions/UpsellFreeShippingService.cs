using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CMS.Commerce;
using CMS.ContentEngine;

using Kentico.Content.Web.Mvc.Routing;
using Kentico.Xperience.Admin.DigitalCommerce;

using Microsoft.Extensions.Localization;

namespace DancingGoat.Commerce;

/// <summary>
/// Service for retrieving upsell free shipping messages based on the current cart content and the active shipping
/// promotions.
/// </summary>
/// <remarks>
/// The service measures the cart against every active free shipping promotion built on the sample
/// <see cref="DancingGoatFreeShippingPromotionRule"/>, picks the promotion the customer reaches with the smallest
/// spend, and words the message by the requirement type (amount, quantity, or item price) and by the scope (whole
/// cart, products, categories, or tags). A promotion built on a custom rule gets no message.
/// </remarks>
public class UpsellFreeShippingService
{
    private readonly PromotionRepository promotionRepository;
    private readonly ProductRepository productRepository;
    private readonly ITaxonomyRetriever taxonomyRetriever;
    private readonly IPreferredLanguageRetriever preferredLanguageRetriever;
    private readonly IPriceFormatter priceFormatter;
    private readonly IStringLocalizer<SharedResources> localizer;


    public UpsellFreeShippingService(
        PromotionRepository promotionRepository,
        ProductRepository productRepository,
        ITaxonomyRetriever taxonomyRetriever,
        IPreferredLanguageRetriever preferredLanguageRetriever,
        IPriceFormatter priceFormatter,
        IStringLocalizer<SharedResources> localizer)
    {
        this.promotionRepository = promotionRepository;
        this.productRepository = productRepository;
        this.taxonomyRetriever = taxonomyRetriever;
        this.preferredLanguageRetriever = preferredLanguageRetriever;
        this.priceFormatter = priceFormatter;
        this.localizer = localizer;
    }


    /// <summary>
    /// Gets an upsell message encouraging the customer to buy more to qualify for free shipping.
    /// </summary>
    /// <param name="calculationResult">The shopping cart calculation result.</param>
    /// <param name="appliedCouponCodes">Coupon codes applied in the shopping cart.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>
    /// The message, or <c>null</c> when the cart already ships for free or when no cart item counts toward an
    /// active free shipping promotion.
    /// </returns>
    public async Task<string> GetUpsellFreeShippingMessage(DancingGoatPriceCalculationResult calculationResult, IEnumerable<string> appliedCouponCodes, CancellationToken cancellationToken)
    {
        if (calculationResult.PromotionData.FreeShippingPromotionCandidates.Any(candidate => candidate.Applied))
        {
            return null;
        }

        var promotions = await promotionRepository.GetActivePromotions(PromotionType.Shipping, appliedCouponCodes, cancellationToken);

        DancingGoatFreeShippingProperties closestProperties = null;
        FreeShippingThresholdProgress closestProgress = null;

        foreach (var promotion in promotions)
        {
            if (!IsSampleRule(promotion))
            {
                continue;
            }

            var properties = promotion.GetPromotionRuleProperties<DancingGoatFreeShippingProperties>();
            if (properties == null)
            {
                continue;
            }

            var progress = FreeShippingThresholdEvaluator.Evaluate(properties, calculationResult.Items);
            if (!IsWithinReach(progress))
            {
                continue;
            }

            if (closestProgress == null || IsCloser(progress, closestProgress))
            {
                closestProperties = properties;
                closestProgress = progress;
            }
        }

        if (closestProperties == null)
        {
            return null;
        }

        return await BuildMessage(closestProperties, closestProgress, cancellationToken);
    }


    private static bool IsSampleRule(PromotionInfo promotion) =>
        string.Equals(promotion.PromotionRuleIdentifier, DancingGoatFreeShippingPromotionRule.IDENTIFIER, StringComparison.InvariantCultureIgnoreCase);


    /// <summary>
    /// The cart does not qualify yet, and at least one cart item counts toward the promotion.
    /// </summary>
    private static bool IsWithinReach(FreeShippingThresholdProgress progress) =>
        !progress.Qualifies && progress.ScopeItemCount > 0;


    /// <summary>
    /// The promotion the customer reaches with a smaller spend wins. The first promotion keeps a tie.
    /// </summary>
    private static bool IsCloser(FreeShippingThresholdProgress candidate, FreeShippingThresholdProgress current) =>
        candidate.MissingAmount < current.MissingAmount;


    private async Task<string> BuildMessage(DancingGoatFreeShippingProperties properties, FreeShippingThresholdProgress progress, CancellationToken cancellationToken)
    {
        var hasProductScope = string.Equals(properties.Scope, DancingGoatFreeShippingPromotionRuleUtils.SCOPE_PRODUCTS, StringComparison.InvariantCultureIgnoreCase);
        var hasCategoryOrTagScope = string.Equals(properties.Scope, DancingGoatFreeShippingPromotionRuleUtils.SCOPE_CATEGORIES, StringComparison.InvariantCultureIgnoreCase)
            || string.Equals(properties.Scope, DancingGoatFreeShippingPromotionRuleUtils.SCOPE_TAGS, StringComparison.InvariantCultureIgnoreCase);

        var scopeDescription = await GetScopeDescription(properties, cancellationToken);
        if ((hasProductScope || hasCategoryOrTagScope) && string.IsNullOrEmpty(scopeDescription))
        {
            return null;
        }

        switch (progress.RequirementType)
        {
            case FreeShippingMinimumRequirementType.Price:
                var missingAmount = priceFormatter.Format(progress.MissingValue, new PriceFormatContext());
                if (hasProductScope)
                {
                    return string.Format(localizer["Spend {0} more on {1} and get free shipping."], missingAmount, scopeDescription);
                }
                if (hasCategoryOrTagScope)
                {
                    return string.Format(localizer["Spend {0} more on {1} products and get free shipping."], missingAmount, scopeDescription);
                }
                return string.Format(localizer["Spend {0} more and get free shipping."], missingAmount);

            case FreeShippingMinimumRequirementType.Quantity:
                var missingQuantity = (int)Math.Ceiling(progress.MissingValue);
                if (hasProductScope)
                {
                    return string.Format(localizer["Add {0} more of {1} and get free shipping."], missingQuantity, scopeDescription);
                }
                if (hasCategoryOrTagScope)
                {
                    return string.Format(localizer["Add {0} more {1} products and get free shipping."], missingQuantity, scopeDescription);
                }
                if (missingQuantity == 1)
                {
                    return localizer["Add one more item and get free shipping."];
                }
                return string.Format(localizer["Add {0} more items and get free shipping."], missingQuantity);

            case FreeShippingMinimumRequirementType.ItemPrice:
                var requiredItemPrice = priceFormatter.Format(progress.TargetValue, new PriceFormatContext());
                if (hasProductScope)
                {
                    return string.Format(localizer["Add one of {1} costing at least {0} and get free shipping."], requiredItemPrice, scopeDescription);
                }
                if (hasCategoryOrTagScope)
                {
                    return string.Format(localizer["Add a {1} product costing at least {0} and get free shipping."], requiredItemPrice, scopeDescription);
                }
                return string.Format(localizer["Add an item costing at least {0} and get free shipping."], requiredItemPrice);

            default:
                // A promotion without a minimum requirement qualifies as soon as a scope item is in the cart. There is nothing to upsell.
                return null;
        }
    }


    /// <summary>
    /// Describes the promotion's scope with product names or tag names. Returns <c>null</c> for the whole-cart scope.
    /// </summary>
    private async Task<string> GetScopeDescription(DancingGoatFreeShippingProperties properties, CancellationToken cancellationToken)
    {
        if (string.Equals(properties.Scope, DancingGoatFreeShippingPromotionRuleUtils.SCOPE_PRODUCTS, StringComparison.InvariantCultureIgnoreCase))
        {
            var products = await productRepository.GetProductsByGuids(properties.Products.Select(product => product.Identifier), cancellationToken);
            return PromotionMessageFormatter.JoinWithOr(localizer, products.Select(product => product.ProductFieldName));
        }

        IEnumerable<TagReference> tagReferences = null;
        if (string.Equals(properties.Scope, DancingGoatFreeShippingPromotionRuleUtils.SCOPE_CATEGORIES, StringComparison.InvariantCultureIgnoreCase))
        {
            tagReferences = properties.ProductCategories;
        }
        else if (string.Equals(properties.Scope, DancingGoatFreeShippingPromotionRuleUtils.SCOPE_TAGS, StringComparison.InvariantCultureIgnoreCase))
        {
            tagReferences = properties.ProductTags;
        }

        if (tagReferences == null)
        {
            return null;
        }

        var languageName = preferredLanguageRetriever.Get();
        var tags = await taxonomyRetriever.RetrieveTags(tagReferences.Select(tag => tag.Identifier), languageName, cancellationToken);

        return PromotionMessageFormatter.JoinWithOr(localizer, tags.Select(tag => tag.Title));
    }
}
