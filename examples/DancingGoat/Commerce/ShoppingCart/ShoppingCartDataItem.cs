namespace DancingGoat.Commerce;

public sealed class ShoppingCartDataItem
{
    /// <summary>
    /// Identifier of the content item representing a product.
    /// </summary>
    public int ContentItemId { get; set; }

    /// <summary>
    /// Quantity of the item in shopping cart.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Identifier of the variant representing specific variant of a product.
    /// </summary>
    public int? VariantId { get; set; }
}
