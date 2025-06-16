using System.Collections.Generic;

namespace DancingGoat.Commerce
{
    /// <summary>
    /// Extractor of product variants based on the product type.
    /// </summary>
    public interface IProductTypeVariantsExtractor
    {
        /// <summary>
        /// Extract product-specific variants value of a product based on it's type and update variants dictionary.
        /// </summary>
        /// <typeparam name="T">Type of the product.</typeparam>
        /// <param name="product">Product to get variants from.</param>
        IDictionary<int, string> ExtractVariantsValue<T>(T product);


        /// <summary>
        /// Extract product-specific SKU code of variants of a product based on it's type and update variants dictionary.
        /// </summary>
        /// <typeparam name="T">Type of the product.</typeparam>
        /// <param name="product">Product to get SKU code variants from.</param>
        IDictionary<int, string> ExtractVariantsSKUCode<T>(T product);
    }
}
