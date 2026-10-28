using System;
using System.Collections.Generic;
using System.Linq;

using CMS.Commerce;

using DancingGoat.Commerce;

[assembly: RegisterPromotionRule<DancingGoatCartItemPromotionRule>(DancingGoatCartItemPromotionRule.IDENTIFIER, PromotionType.CartItem, "{$dancinggoat.cartitempromotionrule.sample.name$}")]

namespace DancingGoat.Commerce;

/// <summary>
/// Represents a cart item promotion rule for the DancingGoat demo site.
/// </summary>
/// <remarks>
/// This is a sample implementation demonstrating how to create a custom cart item promotion rule.
/// It applies promotions to cart items in the promotion rule's scope (categories, tags, or specific product selection) and the quantity purchased.
/// </remarks>
public sealed class DancingGoatCartItemPromotionRule : CartItemPromotionRuleBase<DancingGoatCartItemPromotionRuleProperties, DancingGoatPriceCalculationRequest, DancingGoatPriceCalculationResult>
{
    /// <summary>
    /// Unique identifier for this promotion rule.
    /// </summary>
    public const string IDENTIFIER = "DancingGoatCartItemPromotionRule";


    /// <summary>
    /// Gets the promotion candidates that discount the cheapest units in the promotion rule's scope.
    /// </summary>
    /// <param name="calculationData">Price calculation data containing the cart items.</param>
    /// <returns>
    /// One promotion candidate for every cart line containing a discounted unit. Each candidate holds the quantity of discounted units,
    /// the discount amount taken off the line, and the full-priced units that triggered the discount.
    /// Returns an empty list if the cart items in the promotion rule's scope do not meet the minimum quantity.
    /// </returns>
    public override IReadOnlyList<CartItemPromotionCandidate> GetPromotionCandidates(IPriceCalculationData<DancingGoatPriceCalculationRequest, DancingGoatPriceCalculationResult> calculationData)
    {
        if (Properties.MinimumQuantity < 2)
        {
            return [];
        }

        var scopeItems = calculationData.Result.Items
            .Where(item => DancingGoatCartItemPromotionRuleUtils.IsInScope(Properties, item.ProductData))
            .ToList();

        var scopedUnitsCount = scopeItems.Sum(item => (int)item.Quantity);
        if (scopedUnitsCount < Properties.MinimumQuantity)
        {
            return [];
        }

        var discountedUnitsCount = scopedUnitsCount / Properties.MinimumQuantity;

        return CreateCandidatesForCheapestUnits(scopeItems, discountedUnitsCount);
    }


    /// <summary>
    /// Gets a short label that shows what the promotion takes off the cheapest unit.
    /// </summary>
    /// <returns>The discount percentage.</returns>
    public override string GetDiscountValueLabel() => $"{Properties.DiscountPercentage}%";


    /// <summary>
    /// Discounts the cheapest <paramref name="discountedUnitsCount"/> units in the cart and returns one candidate for
    /// every cart line that holds a discounted unit.
    /// </summary>
    private IReadOnlyList<CartItemPromotionCandidate> CreateCandidatesForCheapestUnits(IEnumerable<IPriceCalculationResultItem<ProductIdentifier, ProductData>> scopeItems, int discountedUnitsCount)
    {
        var discountedLines = new List<(IPriceCalculationResultItem<ProductIdentifier, ProductData> Item, int DiscountedQuantity)>();
        var fullPricedUnits = new List<ProductIdentifier>();

        var unitsToDiscount = discountedUnitsCount;

        foreach (var item in scopeItems.OrderBy(item => item.ProductData.UnitPrice))
        {
            var itemUnits = (int)item.Quantity;

            var discountedQuantity = Math.Min(unitsToDiscount, itemUnits);
            if (discountedQuantity > 0)
            {
                discountedLines.Add((item, discountedQuantity));
                unitsToDiscount -= discountedQuantity;
            }

            var fullPricedQuantity = itemUnits - discountedQuantity;
            for (var i = 0; i < fullPricedQuantity; i++)
            {
                fullPricedUnits.Add(item.ProductIdentifier);
            }
        }

        var fullPricedUnitsPerDiscountedUnit = Properties.MinimumQuantity - 1;
        var takenFullPricedUnitsCount = 0;

        var candidates = new List<CartItemPromotionCandidate>();

        foreach (var (item, discountedQuantity) in discountedLines)
        {
            var triggerUnitsCount = discountedQuantity * fullPricedUnitsPerDiscountedUnit;
            var triggerUnits = fullPricedUnits.Skip(takenFullPricedUnitsCount).Take(triggerUnitsCount);

            takenFullPricedUnitsCount += triggerUnitsCount;

            candidates.Add(new CartItemPromotionCandidate
            {
                ProductIdentifier = item.ProductIdentifier,
                UnitPriceDiscountAmount = item.ProductData.UnitPrice * (Properties.DiscountPercentage / 100m),
                DiscountedQuantity = discountedQuantity,
                PromotionTriggerItems = triggerUnits
                    .GroupBy(productIdentifier => productIdentifier)
                    .Select(group => new PromotionTriggerItem
                    {
                        ProductIdentifier = group.Key,
                        Quantity = group.Count()
                    })
                    .ToList()
            });
        }

        return candidates;
    }
}
