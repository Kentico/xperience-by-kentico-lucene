using CMS.Commerce;

namespace DancingGoat.Commerce;

/// <summary>
/// Custom calculation result item with identifier specifying unique product based on identifier and variant identifier.
/// </summary>
public sealed class DancingGoatPriceCalculationResultItem : PriceCalculationResultItemBase<ProductVariantIdentifier, DancingGoatProductData>
{
}
