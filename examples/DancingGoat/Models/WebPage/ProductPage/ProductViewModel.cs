using System.Collections.Generic;
using System.Linq;

using CMS.Websites;

using DancingGoat.Commerce;

namespace DancingGoat.Models
{
    public record ProductViewModel(string Name, string Description, string ImagePath, decimal Price, decimal ListPrice, DancingGoatCatalogPromotionCandidate AppliedPromotion, ProductListItemTagViewModel Tag, int ContentItemId, IDictionary<string, string> Parameters, IDictionary<int, string> Variants, string Sku, IDictionary<int, string> VariantSkus, bool IsMerchandise, IEnumerable<ProductFreeShippingPromotionViewModel> FreeShippingPromotions, IEnumerable<ProductCartItemDiscountViewModel> CartItemDiscounts)
        : IWebPageBasedViewModel
    {
        /// <inheritdoc/>
        public IWebPageFieldsSource WebPage { get; init; }


        /// <summary>
        /// Indicates whether a catalog promotion discount is applied to the product.
        /// </summary>
        public bool HasDiscount => ListPrice > 0 && ListPrice > Price && AppliedPromotion is not null;


        /// <summary>
        /// SKU code shown when the page opens. Products with variants carry their SKU on the variants,
        /// so the pre-selected variant's code is used; other products use their own SKU.
        /// </summary>
        public string InitialSku => Variants is { Count: > 0 } && VariantSkus.TryGetValue(Variants.Keys.First(), out var variantSku) ? variantSku : Sku;


        /// <summary>
        /// Indicates whether the product itself or any of its variants carries an SKU code.
        /// </summary>
        public bool HasSku => !string.IsNullOrEmpty(Sku) || VariantSkus.Values.Any(variantSkuCode => !string.IsNullOrEmpty(variantSkuCode));
    }
}
