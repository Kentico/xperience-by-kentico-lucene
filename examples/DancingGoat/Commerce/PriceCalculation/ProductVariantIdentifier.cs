using CMS.Commerce;

namespace DancingGoat.Commerce;

/// <summary>
/// Identifier for product with unqiue variants.
/// </summary>
/// <remarks>
/// Unique object is specified by both <see cref="ProductIdentifier.Identifier"/> and <see cref="VariantIdentifier"/>.
/// </remarks>
public record ProductVariantIdentifier : ProductIdentifier
{
    /// <summary>
    /// Variant identifier.
    /// </summary>
    public int? VariantIdentifier { get; init; }
}
