using System.Linq;

using DancingGoat.Commerce;

namespace DancingGoat.Models
{
    public record ProductListItemViewModel(string Name, string ImagePath, string Url, decimal Price, decimal ListPrice, DancingGoatCatalogPromotionCandidate AppliedPromotion, string Tag)
    {
        public static ProductListItemViewModel GetViewModel(IProductFields product, DancingGoatPriceCalculationResultItem calculationResultItem, string urlPath, string tag)
        {
            var appliedPromotion = calculationResultItem.PromotionData.CatalogPromotionCandidates.FirstOrDefault(c => c.Applied)?.PromotionCandidate as DancingGoatCatalogPromotionCandidate;

            return new ProductListItemViewModel(
                            product.ProductFieldName,
                            product.ProductFieldImage.FirstOrDefault()?.ImageFile.Url,
                            urlPath,
                            calculationResultItem?.LineSubtotalAfterLineDiscount ?? product.ProductFieldPrice,
                            product.ProductFieldPrice,
                            appliedPromotion,
                            tag);
        }

        public static ProductListItemViewModel GetViewModel(IProductFields product, string urlPath, string tag)
        {
            return new ProductListItemViewModel(
                            product.ProductFieldName,
                            product.ProductFieldImage.FirstOrDefault()?.ImageFile.Url,
                            urlPath,
                            product.ProductFieldPrice,
                            product.ProductFieldPrice,
                            null,
                            tag);
        }
    }
}
