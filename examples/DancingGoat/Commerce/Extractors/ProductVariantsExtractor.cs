using System.Collections.Generic;
using System.Linq;

using CMS.ContentEngine;

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
        /// Indicates whether the product has any variants.
        /// </summary>
        /// <param name="product">Product to process.</param>
        /// <returns><c>true</c> when at least one variant exists.</returns>
        /// <remarks>
        /// Stops at the first extractor that yields a variant instead of merging them all, so
        /// callers that only need the flag do not pay for building the full dictionary.
        /// </remarks>
        public bool HasVariants(IProductFields product)
        {
            foreach (var item in parametersExtractors)
            {
                var variants = item.ExtractVariantsValue(product);
                if (variants?.Count > 0)
                {
                    return true;
                }
            }

            return false;
        }


        /// <summary>
        /// Extract product variants SKU code and update the dictionary of variants.
        /// </summary>
        /// <param name="product">Product to process.</param>
        /// <returns>Dictionary containing product variant identifier as a key and product variant SKU code as a value.</returns>
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


        /// <summary>
        /// Gets content item identifiers of the products that have at least one variant.
        /// </summary>
        /// <param name="products">Products to process.</param>
        /// <returns>Set of content item identifiers of products with variants.</returns>
        public ISet<int> GetProductIdsWithVariants(IEnumerable<IProductFields> products)
        {
            return products
                .Where(HasVariants)
                .Select(product => ((IContentItemFieldsSource)product).SystemFields.ContentItemID)
                .ToHashSet();
        }
    }
}
