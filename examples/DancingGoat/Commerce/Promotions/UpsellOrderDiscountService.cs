using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CMS.Commerce;

using Kentico.Xperience.Admin.DigitalCommerce;

using Microsoft.Extensions.Localization;

namespace DancingGoat.Commerce;

/// <summary>
/// Service for retrieving upsell order discount messages based on current cart subtotal and available promotions.
/// </summary>
/// <remarks>
/// This service analyzes active order promotions and generates messages encouraging customers to spend more
/// to qualify for the next available discount promotion.
/// </remarks>
public class UpsellOrderDiscountService
{
    private readonly PromotionRepository promotionRepository;
    private readonly IPriceFormatter priceFormatter;
    private readonly IStringLocalizer<DancingGoatShoppingCartController> localizer;


    public UpsellOrderDiscountService(
        PromotionRepository promotionRepository,
        IPriceFormatter priceFormatter,
        IStringLocalizer<DancingGoatShoppingCartController> localizer)
    {
        this.promotionRepository = promotionRepository;
        this.priceFormatter = priceFormatter;
        this.localizer = localizer;
    }


    /// <summary>
    /// Gets an upsell message encouraging the customer to spend more to qualify for the next available order discount promotion.
    /// </summary>
    /// <param name="subtotalAfterLineDiscount">The current cart subtotal amount after line discounts (catalog discounts) were applied.</param>
    /// <param name="currentlyAppliedOrderDiscountAmount">The amount of currently applied order-level discount.</param>
    /// <param name="currentlyAppliedOrderPromotionId">Promotion ID of currently applied order promotion.</param>
    /// <param name="appliedCouponCodes">List of coupon codes currently applied in the shopping cart.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <example>
    /// Example return value: "Spend $25.00 more and get 10% discount."
    /// </example>
    public async Task<string> GetUpsellOrderDiscountMessage(decimal subtotalAfterLineDiscount, decimal currentlyAppliedOrderDiscountAmount, int? currentlyAppliedOrderPromotionId, IEnumerable<string> appliedCouponCodes, CancellationToken cancellationToken)
    {
        var (nextOrderDiscountRemainingTreshold, nextOrderDiscountValue) = await GetNextEligibleOrderPromotion(subtotalAfterLineDiscount, currentlyAppliedOrderDiscountAmount, currentlyAppliedOrderPromotionId, appliedCouponCodes, cancellationToken);

        if ((nextOrderDiscountRemainingTreshold > 0) && !string.IsNullOrEmpty(nextOrderDiscountValue))
        {
            var nextDiscountTextResource = localizer["Spend {0} more and get {1} discount."];
            var nextDiscountText = string.Format(nextDiscountTextResource, priceFormatter.Format(nextOrderDiscountRemainingTreshold, new PriceFormatContext()), nextOrderDiscountValue);
            return nextDiscountText;
        }

        return null;
    }


    /// <summary>
    /// Gets the next eligible order promotion that the customer can qualify for by spending more.
    /// </summary>
    /// <param name="subtotalAfterLineDiscount">The current cart subtotal amount after line discounts (catalog discounts) were applied.</param>
    /// <param name="currentlyAppliedOrderDiscountAmount">The amount of currently applied order-level discount.</param>
    /// <param name="currentlyAppliedOrderPromotionId">Promotion ID of currently applied order promotion.</param>
    /// <param name="appliedCouponCodes">List of coupon codes currently applied in the shopping cart.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>
    /// A tuple containing:
    /// - Item1: The amount the customer needs to spend more to qualify (decimal).
    /// - Item2: A formatted label representing the discount value (e.g., "10%" or "$25.00").
    /// Returns default tuple (0, null) if no eligible promotion is found.
    /// </returns>
    private async Task<(decimal, string)> GetNextEligibleOrderPromotion(decimal subtotalAfterLineDiscount, decimal currentlyAppliedOrderDiscountAmount, int? currentlyAppliedOrderPromotionId, IEnumerable<string> appliedCouponCodes, CancellationToken cancellationToken)
    {
        var activeOrderPromotions = (await promotionRepository.GetActivePromotions(PromotionType.Order, appliedCouponCodes, cancellationToken))
            .Where(promotion => string.Equals(promotion.PromotionRuleIdentifier, DancingGoatOrderPromotionRule.IDENTIFIER, StringComparison.InvariantCultureIgnoreCase));
        var promotionRulePropertiesById = ExtractPromotionRulePropertiesByPromotionId(activeOrderPromotions);
        promotionRulePropertiesById.TryGetValue(currentlyAppliedOrderPromotionId ?? 0, out var currentlyAppliedPromotionRuleProperties);

        var fixedValuesBasedPromotionProperties = promotionRulePropertiesById
                                                        .Where(p => p.Value.MinimumRequirementValueType == MinimumRequirementValueType.Price)
                                                        .Where(p => IsBetterThanCurrentPromotion(p.Value, currentlyAppliedOrderDiscountAmount, currentlyAppliedPromotionRuleProperties))
                                                        .OrderBy(p => p.Value.MinimumRequirementValue)
                                                        .Select(p => p.Value);

        var nextAvailablePromotion = fixedValuesBasedPromotionProperties.FirstOrDefault(p => p.MinimumRequirementValue > subtotalAfterLineDiscount);

        if (nextAvailablePromotion == null)
        {
            return default;
        }

        var label = GetNextDiscountValueLabel(nextAvailablePromotion);
        var amountToSpend = nextAvailablePromotion.MinimumRequirementValue - subtotalAfterLineDiscount;

        return (amountToSpend, label);
    }


    private static decimal GetDiscountAmountForAmount(OrderPromotionRuleProperties promotionRuleProperties, decimal amount)
    {
        if (promotionRuleProperties.DiscountValueType == DiscountValueType.Percentage)
        {
            return amount * promotionRuleProperties.DiscountValue / 100;
        }

        return promotionRuleProperties.DiscountValue;
    }


    private static bool IsBetterThanCurrentPromotion(
        OrderPromotionRuleProperties candidatePromotionRuleProperties,
        decimal currentlyAppliedOrderDiscountAmount,
        OrderPromotionRuleProperties currentlyAppliedPromotionRuleProperties)
    {
        // Compare both promotions on the same amount to avoid false upsell for "lower percentage at higher threshold" cases.
        if (currentlyAppliedPromotionRuleProperties != null)
        {
            var comparisonAmount = candidatePromotionRuleProperties.MinimumRequirementValue;
            var currentDiscountOnComparisonAmount = GetDiscountAmountForAmount(currentlyAppliedPromotionRuleProperties, comparisonAmount);
            var candidateDiscountOnComparisonAmount = GetDiscountAmountForAmount(candidatePromotionRuleProperties, comparisonAmount);

            return candidateDiscountOnComparisonAmount > currentDiscountOnComparisonAmount;
        }

        return GetDiscountAmountForAmount(candidatePromotionRuleProperties, candidatePromotionRuleProperties.MinimumRequirementValue) > currentlyAppliedOrderDiscountAmount;
    }


    /// <summary>
    /// Gets the next discount value label for the next eligible order promotion.
    /// </summary>
    /// <param name="promotionRuleProperties">The promotion rule properties of the next eligible order promotion.</param>
    /// <returns>The next discount value label.</returns>
    private static string GetNextDiscountValueLabel(OrderPromotionRuleProperties promotionRuleProperties)
    {
        var discountRule = new DancingGoatOrderPromotionRule();
        discountRule.Properties.DiscountValue = promotionRuleProperties.DiscountValue;
        discountRule.Properties.DiscountValueType = promotionRuleProperties.DiscountValueType;
        return discountRule.GetDiscountValueLabel();
    }


    /// <summary>
    /// Extracts order promotion rule properties from a collection of promotion information objects.
    /// </summary>
    /// <param name="promotions">Collection of promotion information objects.</param>
    /// <returns>
    /// A collection of <see cref="OrderPromotionRuleProperties"/> extracted from the promotion configurations.
    /// </returns>
    private static Dictionary<int, OrderPromotionRuleProperties> ExtractPromotionRulePropertiesByPromotionId(IEnumerable<PromotionInfo> promotions)
    {
        var promotionPropertiesById = new Dictionary<int, OrderPromotionRuleProperties>();
        foreach (var promotion in promotions)
        {
            var promotionRuleProperties = promotion.GetPromotionRuleProperties<OrderPromotionRuleProperties>();

            if (promotionRuleProperties != null)
            {
                promotionPropertiesById[promotion.PromotionID] = promotionRuleProperties;
            }
        }

        return promotionPropertiesById;
    }
}
