using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DancingGoat.Commerce
{
    /// <summary>
    /// Extractor of product parameters based on the product type.
    /// </summary>
    public interface IProductTypeParametersExtractor
    {
        /// <summary>
        /// Extract product-specific parameters of a product based on it's type and updates parameters dictionary.
        /// </summary>
        /// <typeparam name="T">Type of the product.</typeparam>
        /// <param name="parameters">Dictionary containing parameters of the product that will be updated.</param>
        /// <param name="product">Product to get parameters from.</param>
        /// <param name="languageName">Language name to use.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task ExtractParameter<T>(IDictionary<string, string> parameters, T product, string languageName, CancellationToken cancellationToken);
    }
}
