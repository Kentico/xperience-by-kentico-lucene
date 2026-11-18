using System.Collections.Generic;

namespace DancingGoat.Models;

/// <summary>
/// Represents an item in the shopping cart.
/// </summary>
/// <param name="ContentItemId">The ID of the content item.</param>
/// <param name="Name">The name of the cart item.</param>
/// <param name="ImageUrl">The URL of the item's image.</param>
/// <param name="DetailUrl">The URL of the item's detail page.</param>
/// <param name="Quantity">The quantity of the item in the cart.</param>
/// <param name="TotalPrice">The total price of the item in the cart.</param>
/// <param name="ListPrice">The undiscounted list price of the item.</param>
/// <param name="VariantId">The ID of the item's variant, if applicable.</param>
/// <param name="Sku">The SKU of the item.</param>
public record ShoppingCartItemViewModel(int ContentItemId, string Name, string ImageUrl, string DetailUrl, int Quantity, decimal TotalPrice, decimal ListPrice, int? VariantId, string Sku = null)
{
    /// <summary>
    /// Indicates whether a catalog or a cart item promotion discount lowers the line total.
    /// </summary>
    public bool HasDiscount => ListPrice > TotalPrice;


    /// <summary>
    /// Gets the undiscounted price of a single unit, shown as the per-unit price on a cart line.
    /// Derived from <c>ListPrice</c>, which is the undiscounted price of the whole line
    /// (unit list price multiplied by quantity). A discount is shown on the line total instead,
    /// where <c>ListPrice</c> is the struck-through value.
    /// </summary>
    public decimal UnitListPrice => Quantity > 0 ? ListPrice / Quantity : ListPrice;


    /// <summary>
    /// Gets the promotions applied to the cart item, one row per promotion, catalog promotion first.
    /// Empty unless both a catalog and a cart item promotion discount the line, because only then the units of the line have
    /// different prices. A line with a single promotion shows its original and discounted price instead.
    /// </summary>
    public IReadOnlyList<PromotionBreakdown> PromotionBreakdowns { get; init; } = [];
}
