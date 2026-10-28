using System;
using System.Collections.Generic;
using System.Linq;

using Kentico.Xperience.Admin.DigitalCommerce;

namespace DancingGoat.Commerce;

/// <summary>
/// Evaluates price calculation items against the scope and minimum requirement of the DancingGoat free shipping
/// promotion rule.
/// </summary>
public static class FreeShippingThresholdEvaluator
{
    /// <summary>
    /// Determines whether the given items, taken together, meet the promotion's scope and minimum requirement.
    /// </summary>
    /// <param name="properties">Free shipping promotion rule properties to evaluate against.</param>
    /// <param name="items">Price calculation items to evaluate.</param>
    public static bool Qualifies(DancingGoatFreeShippingProperties properties, IEnumerable<DancingGoatPriceCalculationResultItem> items) =>
        FreeShippingPromotionRuleUtils.MeetsMinimumRequirement(properties, GetScopeItems(properties, items));


    /// <summary>
    /// Determines whether the given item, alone in its own quantity, meets the promotion's scope and minimum
    /// requirement.
    /// </summary>
    /// <param name="properties">Free shipping promotion rule properties to evaluate against.</param>
    /// <param name="item">Price calculation item to evaluate.</param>
    public static bool QualifiesAlone(DancingGoatFreeShippingProperties properties, DancingGoatPriceCalculationResultItem item) =>
        Qualifies(properties, [item]);


    /// <summary>
    /// Measures how far the given items are from the promotion's minimum requirement.
    /// </summary>
    /// <param name="properties">Free shipping promotion rule properties to evaluate against.</param>
    /// <param name="items">Price calculation items to evaluate.</param>
    /// <remarks>
    /// The current value mirrors <see cref="FreeShippingPromotionRuleUtils.MeetsMinimumRequirement"/>: the line
    /// subtotal after all discounts for a price requirement, the total quantity for a quantity requirement, and
    /// the highest unit price after all discounts for an item price requirement.
    /// </remarks>
    public static FreeShippingThresholdProgress Evaluate(DancingGoatFreeShippingProperties properties, IEnumerable<DancingGoatPriceCalculationResultItem> items)
    {
        var scopeItems = GetScopeItems(properties, items);

        (decimal targetValue, decimal currentValue) = properties.MinimumRequirementValueType switch
        {
            FreeShippingMinimumRequirementType.None => (1, scopeItems.Count),
            FreeShippingMinimumRequirementType.Quantity => (properties.MinimumRequirementValue, scopeItems.Sum(item => item.Quantity)),
            FreeShippingMinimumRequirementType.Price => (properties.MinimumRequirementValue, scopeItems.Sum(item => item.LineSubtotalAfterAllDiscounts)),
            FreeShippingMinimumRequirementType.ItemPrice => (properties.MinimumRequirementValue, scopeItems.Count > 0 ? scopeItems.Max(GetUnitPriceAfterAllDiscounts) : 0),
            _ => (0, 0)
        };

        var qualifies = FreeShippingPromotionRuleUtils.MeetsMinimumRequirement(properties, scopeItems);

        return new FreeShippingThresholdProgress
        {
            RequirementType = properties.MinimumRequirementValueType,
            TargetValue = targetValue,
            CurrentValue = currentValue,
            Qualifies = qualifies,
            ScopeItemCount = scopeItems.Count,
            MissingAmount = qualifies ? 0 : GetMissingAmount(properties.MinimumRequirementValueType, targetValue, currentValue, scopeItems)
        };
    }


    /// <summary>
    /// Estimates the amount the customer must spend to meet the requirement.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    ///<item>A quantity requirement - calculates the missing amount based on the scope of items in the cart and uses the cheapest one to approximate the missing amount.</item>
    ///<item>An item price requirement - returns the required item price, because the customer must add a new item at that price.</item>
    /// </list>
    /// </remarks>
    private static decimal GetMissingAmount(FreeShippingMinimumRequirementType requirementType, decimal targetValue, decimal currentValue, List<DancingGoatPriceCalculationResultItem> scopeItems)
    {
        var missingValue = Math.Max(0, targetValue - currentValue);

        return requirementType switch
        {
            FreeShippingMinimumRequirementType.Price => missingValue,
            FreeShippingMinimumRequirementType.Quantity => Math.Ceiling(missingValue) * (scopeItems.Count > 0 ? scopeItems.Min(GetUnitPriceAfterAllDiscounts) : 0),
            FreeShippingMinimumRequirementType.ItemPrice => targetValue,
            _ => 0
        };
    }


    private static List<DancingGoatPriceCalculationResultItem> GetScopeItems(DancingGoatFreeShippingProperties properties, IEnumerable<DancingGoatPriceCalculationResultItem> items) =>
        items.Where(item => DancingGoatFreeShippingPromotionRuleUtils.IsInScope(properties, item.ProductData)).ToList();


    private static decimal GetUnitPriceAfterAllDiscounts(DancingGoatPriceCalculationResultItem item) =>
        item.Quantity > 0 ? item.LineSubtotalAfterAllDiscounts / item.Quantity : 0;
}
