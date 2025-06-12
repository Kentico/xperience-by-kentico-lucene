using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using DancingGoat.Models;

namespace DancingGoat.Commerce
{
    /// <summary>
    /// Extractor of product-specific parameters.
    /// </summary>
    public sealed class ProductParametersExtractor
    {
        private readonly IEnumerable<IProductTypeParametersExtractor> parametersExtractors;


        public ProductParametersExtractor(IEnumerable<IProductTypeParametersExtractor> parametersExtractors)
        {
            this.parametersExtractors = parametersExtractors;
        }


        /// <summary>
        /// Extract product parameters and update the dictionary of parameters.
        /// </summary>
        /// <param name="product">Product to process.</param>
        /// <param name="languageName">Language name used.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Dictionary containing product parameters.</returns>
        public async Task<IDictionary<string, string>> ExtractParameters(IProductFields product, string languageName, CancellationToken cancellationToken)
        {
            var parameters = new Dictionary<string, string>();

            foreach (var item in parametersExtractors)
            {
                await item.ExtractParameter(parameters, product, languageName, cancellationToken);
            }

            return parameters;
        }
    }
}
