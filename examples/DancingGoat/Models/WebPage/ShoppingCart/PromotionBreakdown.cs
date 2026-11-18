namespace DancingGoat.Models;

/// <summary>
/// One row of a cart line breakdown: the units of the line a single promotion prices.
/// </summary>
/// <param name="PromotionName">Display name of the promotion.</param>
/// <param name="CoveredQuantity">Number of units of the line the promotion prices.</param>
/// <param name="FullPrice">Undiscounted price of the covered units, shown struck through.</param>
/// <param name="DiscountedPrice">Price of the covered units after the promotion.</param>
public record PromotionBreakdown(string PromotionName, decimal CoveredQuantity, decimal FullPrice, decimal DiscountedPrice);
