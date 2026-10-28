using System.Linq;

using CMS.Commerce;

using DancingGoat.Commerce;

using Kentico.Xperience.Admin.DigitalCommerce;

[assembly: RegisterPromotionRule<DancingGoatOrderPromotionRule>(DancingGoatOrderPromotionRule.IDENTIFIER, PromotionType.Order, "{$dancinggoat.orderpromotionrule.sample.name$}")]

namespace DancingGoat.Commerce;

/// <summary>
/// Represents an order promotion rule for DancingGoat demo site.
/// </summary>
/// <remarks>
/// This is a sample implementation demonstrating how to create a custom order promotion rule.
/// It applies promotions to the entire order based on the sum of line item subtotals after line item discounts have been applied.
/// The discount is calculated using the base class's <see cref="OrderPromotionRule{TPromotionRuleProperties, TPriceCalculationRequest, TPriceCalculationResult}.GetDiscountAmount"/> method,
/// which supports both percentage and fixed amount discounts based on the promotion rule properties.
/// </remarks>
public class DancingGoatOrderPromotionRule : OrderPromotionRule<OrderPromotionRuleProperties, DancingGoatPriceCalculationRequest, DancingGoatPriceCalculationResult>
{
    /// <summary>
    /// Unique identifier for this promotion rule.
    /// </summary>
    public const string IDENTIFIER = "DancingGoatOrderPromotionRule";


    /// <summary>
    /// Gets the promotion candidate that can be used for the whole order.
    /// </summary>
    /// <param name="calculationData">Price calculation data containing the order information.</param>
    /// <returns>
    /// Promotion candidate with the order promotion amount calculated based on the sum of all line item subtotals after line item discounts.
    /// The discount amount is computed using the promotion rule's discount configuration (percentage or fixed amount).
    /// </returns>
    public override OrderPromotionCandidate GetPromotionCandidate(IPriceCalculationData<DancingGoatPriceCalculationRequest, DancingGoatPriceCalculationResult> calculationData)
    {
        var totalPrice = calculationData.Result.Items.Select(i => i.LineSubtotalAfterLineDiscount).Sum();

        return new OrderPromotionCandidate()
        {
            OrderDiscountAmount = GetDiscountAmount(totalPrice)
        };
    }
}
