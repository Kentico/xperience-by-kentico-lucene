using System;
using System.Collections.Generic;
using System.Linq;

using CMS.Commerce;

using DancingGoat.Commerce;

using Kentico.Xperience.Admin.DigitalCommerce;

[assembly: RegisterPromotionRule<DancingGoatFreeShippingPromotionRule>(DancingGoatFreeShippingPromotionRule.IDENTIFIER, PromotionType.Shipping, "{$dancinggoat.freeshippingpromotionrule.sample.name$}")]

namespace DancingGoat.Commerce;

/// <summary>
/// Represents a free shipping promotion rule for DancingGoat demo site.
/// </summary>
/// <remarks>
/// This is a sample implementation demonstrating how to create a custom free shipping promotion rule.
/// It ships the order for free if the cart items in the promotion rule's scope (any product, categories, tags,
/// or specific product selection) meet a purchase threshold.
/// The threshold is evaluated using the base class's <see cref="FreeShippingPromotionRule{TPromotionRuleProperties, TPriceCalculationRequest, TPriceCalculationResult}.MeetsMinimumRequirement"/> method,
/// which supports a minimum purchase amount, a minimum quantity of items, or a minimum item price.
/// </remarks>
public sealed class DancingGoatFreeShippingPromotionRule : FreeShippingPromotionRule<DancingGoatFreeShippingProperties, DancingGoatPriceCalculationRequest, DancingGoatPriceCalculationResult>
{
    /// <summary>
    /// Unique identifier for this promotion rule.
    /// </summary>
    public const string IDENTIFIER = "DancingGoatFreeShippingPromotionRule";


    /// <summary>
    /// Gets the promotion candidate that ships the order for free.
    /// </summary>
    /// <param name="calculationData">Price calculation data containing the cart items.</param>
    /// <returns>
    /// Promotion candidate with the products that qualified the customer, <c>null</c> if the cart items in the
    /// promotion rule's scope do not meet the purchase threshold.
    /// </returns>
    public override FreeShippingPromotionCandidate GetPromotionCandidate(
        IPriceCalculationData<DancingGoatPriceCalculationRequest, DancingGoatPriceCalculationResult> calculationData)
    {
        var scopeItems = calculationData.Result.Items.Where(item => DancingGoatFreeShippingPromotionRuleUtils.IsInScope(Properties, item.ProductData)).ToList();

        if (!MeetsMinimumRequirement(scopeItems))
        {
            return null;
        }

        return new FreeShippingPromotionCandidate()
        {
            TriggerProductIdentifiers = GetTriggerProductIdentifiers(scopeItems)
        };
    }


    /// <summary>
    /// Returns the products that earned the reward.
    /// </summary>
    /// <param name="scopeItems">The cart items in the promotion rule's scope.</param>
    private IReadOnlyList<ProductIdentifier> GetTriggerProductIdentifiers(IEnumerable<IPriceCalculationResultItem<ProductIdentifier, ProductData>> scopeItems)
    {
        var isAnyProductScope = Properties.Scope.Equals(DancingGoatFreeShippingPromotionRuleUtils.SCOPE_ANY_PRODUCT, StringComparison.InvariantCultureIgnoreCase);

        // A whole-cart threshold is earned by the cart as a whole, not by a product.
        if (isAnyProductScope && Properties.MinimumRequirementValueType != FreeShippingMinimumRequirementType.ItemPrice)
        {
            return [];
        }

        return [.. GetItemsMeetingMinimumRequirement(scopeItems).Select(item => item.ProductIdentifier)];
    }
}
