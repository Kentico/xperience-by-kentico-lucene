using System.Linq;

using CMS.ContentEngine;

using DancingGoat.Commerce;

namespace DancingGoat.Models
{
    public record ProductListItemViewModel(string Name, string ImagePath, string Url, decimal Price, decimal ListPrice, DancingGoatCatalogPromotionCandidate AppliedPromotion, ProductListItemTagViewModel Tag, int ContentItemId, bool HasVariants, bool HasFreeShipping, bool HasSpecialOffer)
    {
        /// <summary>
        /// Indicates whether a catalog promotion discount is applied to the product.
        /// </summary>
        public bool HasDiscount => ListPrice > 0 && ListPrice > Price && AppliedPromotion is not null;


        public static ProductListItemViewModel GetViewModel(IProductFields product, DancingGoatPriceCalculationResultItem calculationResultItem, string urlPath, ProductListItemTagViewModel tag, bool hasVariants, bool hasFreeShipping, bool hasSpecialOffer)
        {
            var appliedPromotion = calculationResultItem?.PromotionData.CatalogPromotionCandidates.FirstOrDefault(c => c.Applied)?.PromotionCandidate as DancingGoatCatalogPromotionCandidate;

            return new ProductListItemViewModel(
                            product.ProductFieldName,
                            product.ProductFieldImage.FirstOrDefault()?.ImageFile.Url,
                            urlPath,
                            calculationResultItem?.LineSubtotalAfterLineDiscount ?? product.ProductFieldPrice,
                            product.ProductFieldPrice,
                            appliedPromotion,
                            tag,
                            (product as IContentItemFieldsSource).SystemFields.ContentItemID,
                            hasVariants,
                            hasFreeShipping,
                            hasSpecialOffer);
        }
    }
}
