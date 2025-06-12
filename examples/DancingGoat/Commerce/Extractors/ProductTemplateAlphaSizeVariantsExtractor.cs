using System.Collections.Generic;
using System.Linq;

using DancingGoat.Models;

namespace DancingGoat.Commerce
{
    /// <inheritdoc cref="IProductTypeVariantsExtractor"/>
    internal class ProductTemplateAlphaSizeVariantsExtractor : IProductTypeVariantsExtractor
    {
        /// <inheritdoc/>
        public IDictionary<int, string> ExtractVariantsValue<T>(T product)
        {
            if (product is ProductTemplateAlphaSize productTemplateAlphaSize)
            {
                return productTemplateAlphaSize.ProductVariants.ToDictionary(x => x.SystemFields.ContentItemID, x => x.ProductOptionAlphaSize);
            }
            return null;
        }


        /// <inheritdoc/>
        public IDictionary<int, string> ExtractVariantsSKUCode<T>(T product)
        {
            if (product is ProductTemplateAlphaSize productTemplateAlphaSize)
            {
                return productTemplateAlphaSize.ProductVariants.ToDictionary(x => x.SystemFields.ContentItemID, x => x.ProductSKUCode);
            }
            return null;
        }
    }
}
