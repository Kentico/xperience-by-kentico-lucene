namespace DancingGoat.Commerce;

public sealed class ShoppingCartDataItem
{
    /// <summary>
    /// Identifier holding content item identifier and variant identifier if applicable (variant identifier represents specific variant of a product).
    /// </summary>
    public ProductVariantIdentifier ProductIdentifier { get; set; }

    /// <summary>
    /// Quantity of the item in shopping cart.
    /// </summary>
    public int Quantity { get; set; }
}
