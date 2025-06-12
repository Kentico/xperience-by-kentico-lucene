using System.Collections.Generic;

using DancingGoat.Models;

namespace DancingGoat.Commerce
{
    /// <summary>
    /// Extractor of product-specific variants.
    /// </summary>
    public sealed class ProductVariantsExtractor
    {
        private readonly IEnumerable<IProductTypeVariantsExtractor> parametersExtractors;


        public ProductVariantsExtractor(IEnumerable<IProductTypeVariantsExtractor> parametersExtractors)
        {
            this.parametersExtractors = parametersExtractors;
        }


        /// <summary>
        /// Extract product variants and update the dictionary of variants.
        /// </summary>
        /// <param name="product">Product to process.</param>
        /// <returns>Dictionary containing product variants.</returns>
        public IDictionary<int, string> ExtractVariantsValue(IProductFields product)
        {
            var result = new Dictionary<int, string>();

            foreach (var item in parametersExtractors)
            {
                var variants = item.ExtractVariantsValue(product);
                if (variants != null)
                {
                    foreach (var variant in variants)
                    {
                        result.Add(variant.Key, variant.Value);
                    }
                }
            }

            return result;
        }


        /// <summary>
        /// Extract product variants SKU code and update the dictionary of variants.
        /// </summary>
        /// <param name="product">Product to process.</param>
        /// <returns>Dictionary containing product variants SKU code.</returns>
        public IDictionary<int, string> ExtractVariantsSKUCode(IProductFields product)
        {
            var result = new Dictionary<int, string>();

            foreach (var item in parametersExtractors)
            {
                var variants = item.ExtractVariantsSKUCode(product);
                if (variants != null)
                {
                    foreach (var variant in variants)
                    {
                        result.Add(variant.Key, variant.Value);
                    }
                }
            }

            return result;
        }
    }
}
