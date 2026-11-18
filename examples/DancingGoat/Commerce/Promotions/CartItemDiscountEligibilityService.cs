using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CMS.Commerce;

using DancingGoat.Models;

using Microsoft.Extensions.Localization;

namespace DancingGoat.Commerce;

/// <summary>
/// Tells which products count toward an active cart item discount, so that the product listing can mark them
/// with the special offer badge, and builds the discount messages the product detail advertises.
/// </summary>
/// <remarks>
/// The service evaluates every active cart item promotion that has no coupon and uses the
/// <see cref="DancingGoatCartItemPromotionRule"/>. The listing badge is shown also for promotions reserved for
/// registered customers when the visitor is not signed in, so that the offer motivates the visitor to sign in.
/// The product detail then tells the visitor that the promotion requires signing in.
/// Promotions with a different promotion rule are not evaluated.
/// The badge does not change the listing price, because the discount depends on the content of the cart.
/// </remarks>
public sealed class CartItemDiscountEligibilityService
{
    private readonly PromotionRepository promotionRepository;
    private readonly IPromotionCustomerEligibilityValidator promotionCustomerEligibilityValidator;
    private readonly BuyerIdentifierRetriever buyerIdentifierRetriever;
    private readonly IStringLocalizer<SharedResources> localizer;


    /// <summary>
    /// Creates a new instance of <see cref="CartItemDiscountEligibilityService"/>.
    /// </summary>
    public CartItemDiscountEligibilityService(
        PromotionRepository promotionRepository,
        IPromotionCustomerEligibilityValidator promotionCustomerEligibilityValidator,
        BuyerIdentifierRetriever buyerIdentifierRetriever,
        IStringLocalizer<SharedResources> localizer)
    {
        this.promotionRepository = promotionRepository;
        this.promotionCustomerEligibilityValidator = promotionCustomerEligibilityValidator;
        this.buyerIdentifierRetriever = buyerIdentifierRetriever;
        this.localizer = localizer;
    }


    /// <summary>
    /// Returns content item identifiers of listed products that are in the qualifying scope of an active cart item
    /// promotion, including promotions reserved for registered customers.
    /// </summary>
    /// <param name="calculationResultItems">Catalog price calculation items of the listed products.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    public async Task<ISet<int>> GetSpecialOfferProductIds(IEnumerable<DancingGoatPriceCalculationResultItem> calculationResultItems, CancellationToken cancellationToken)
    {
        var promotionProperties = await GetActivePromotionProperties(cancellationToken);
        var specialOfferProductIds = new HashSet<int>();

        if (promotionProperties.Count == 0)
        {
            return specialOfferProductIds;
        }

        foreach (var item in calculationResultItems)
        {
            if (promotionProperties.Any(properties => DancingGoatCartItemPromotionRuleUtils.IsInScope(properties, item.ProductData)))
            {
                specialOfferProductIds.Add(item.ProductIdentifier.Identifier);
            }
        }

        return specialOfferProductIds;
    }


    /// <summary>
    /// Gets the active cart item promotions the product counts toward, each with a message telling the customer
    /// how to get the discount with the product.
    /// </summary>
    /// <param name="calculationResultItem">Catalog calculation item of the product.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>
    /// One view model per advertised promotion. The message of a coupon-activated promotion names the coupon code,
    /// so that the customer learns about it. Promotions with a different promotion rule are left out, because the
    /// storefront cannot know what they require.
    /// </returns>
    /// <remarks>
    /// The messages are built from the promotion configuration and from the product data the product detail
    /// has already loaded, so no additional price calculation runs.
    /// </remarks>
    public async Task<IReadOnlyList<ProductCartItemDiscountViewModel>> GetCartItemDiscounts(DancingGoatPriceCalculationResultItem calculationResultItem, CancellationToken cancellationToken)
    {
        var promotions = await promotionRepository.GetAvailablePromotions(PromotionType.CartItem, DancingGoatCartItemPromotionRule.IDENTIFIER, cancellationToken);
        if (!promotions.Any())
        {
            return [];
        }

        var activatingCouponCodes = await promotionRepository.GetActivatingCouponCodes(promotions.Select(promotion => promotion.PromotionID), cancellationToken);
        var buyerIdentifier = await buyerIdentifierRetriever.GetCurrentBuyerIdentifier();
        var advertisedPromotions = new List<ProductCartItemDiscountViewModel>();

        foreach (var promotion in promotions)
        {
            var properties = promotion.GetPromotionRuleProperties<DancingGoatCartItemPromotionRuleProperties>();
            if (!DancingGoatCartItemPromotionRuleUtils.IsInScope(properties, calculationResultItem.ProductData))
            {
                continue;
            }

            string message = GetPromotionMessage(properties);

            if (activatingCouponCodes.TryGetValue(promotion.PromotionID, out string couponCode))
            {
                message = $"{message} {string.Format(localizer["Use the coupon code '{0}' in shopping cart."], couponCode)}";
            }

            if (promotion.PromotionCustomerEligibility.Equals(PromotionCustomerEligibility.Registered))
            {
                var eligibilityResult = await promotionCustomerEligibilityValidator.Validate(promotion.PromotionCustomerEligibility, buyerIdentifier, cancellationToken);
                if (!eligibilityResult.IsEligible)
                {
                    message = $"{message} {localizer["Available only to registered customers - sign in to get it."]}";
                }
            }

            advertisedPromotions.Add(new ProductCartItemDiscountViewModel(message));
        }

        return advertisedPromotions;
    }


    private string GetPromotionMessage(DancingGoatCartItemPromotionRuleProperties properties)
    {
        if (properties.DiscountPercentage == 100)
        {
            return string.Format(localizer["Buy {0} or more qualifying products and get the cheapest one free."], properties.MinimumQuantity);
        }

        return string.Format(localizer["Buy {0} or more qualifying products and get the cheapest one {1}% off."], properties.MinimumQuantity, properties.DiscountPercentage.ToString("0.##"));
    }


    /// <summary>
    /// Gets the properties of the active, coupon-free sample rule promotions, regardless of the customer
    /// eligibility of the promotions.
    /// </summary>
    private async Task<IReadOnlyList<DancingGoatCartItemPromotionRuleProperties>> GetActivePromotionProperties(CancellationToken cancellationToken)
    {
        return (await promotionRepository.GetActivePromotions(PromotionType.CartItem, [], cancellationToken))
            .Where(IsSampleRule)
            .Select(promotion => promotion.GetPromotionRuleProperties<DancingGoatCartItemPromotionRuleProperties>() ?? new DancingGoatCartItemPromotionRuleProperties())
            .ToList();
    }


    private static bool IsSampleRule(PromotionInfo promotion) =>
        string.Equals(promotion.PromotionRuleIdentifier, DancingGoatCartItemPromotionRule.IDENTIFIER, StringComparison.InvariantCultureIgnoreCase);
}
