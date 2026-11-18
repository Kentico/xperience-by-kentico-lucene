using System;

using Kentico.Xperience.Admin.DigitalCommerce;

namespace DancingGoat.Commerce;

/// <summary>
/// Distance of a cart from the minimum requirement of a free shipping promotion.
/// </summary>
/// <remarks>
/// Produced by <see cref="FreeShippingThresholdEvaluator.Evaluate"/>. The upsell message reads the distance to the
/// requirement and picks the promotion the customer reaches with the smallest spend.
/// </remarks>
public sealed record FreeShippingThresholdProgress
{
    /// <summary>
    /// The requirement type the promotion counts.
    /// </summary>
    public required FreeShippingMinimumRequirementType RequirementType { get; init; }


    /// <summary>
    /// The value the promotion requires. The unit depends on <see cref="RequirementType"/>: an amount, a quantity,
    /// or a single item price.
    /// </summary>
    public required decimal TargetValue { get; init; }


    /// <summary>
    /// The value the cart items in the promotion's scope reach now, in the same unit as <see cref="TargetValue"/>.
    /// For <see cref="FreeShippingMinimumRequirementType.ItemPrice"/>, the highest unit price among the scope items.
    /// </summary>
    public required decimal CurrentValue { get; init; }


    /// <summary>
    /// The cart meets the requirement.
    /// </summary>
    public required bool Qualifies { get; init; }


    /// <summary>
    /// Number of cart items in the promotion's scope. Zero means no product in the cart counts toward this promotion.
    /// </summary>
    public required int ScopeItemCount { get; init; }


    /// <summary>
    /// The value missing to the requirement. Zero when the cart qualifies.
    /// </summary>
    public decimal MissingValue => Qualifies ? 0 : Math.Max(0, TargetValue - CurrentValue);


    /// <summary>
    /// The estimated amount the customer must spend to meet the requirement. Zero when the cart qualifies.
    /// Comparable across requirement types.
    /// </summary>
    /// <remarks>
    /// For a price requirement, the missing amount. For a quantity requirement, the missing quantity priced at the
    /// cheapest scope item in the cart. For an item price requirement, the required item price.
    /// </remarks>
    public required decimal MissingAmount { get; init; }
}
