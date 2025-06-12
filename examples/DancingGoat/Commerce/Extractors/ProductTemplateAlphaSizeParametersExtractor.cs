using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using DancingGoat.Models;

namespace DancingGoat.Commerce
{
    /// <inheritdoc cref="IProductTypeParametersExtractor"/>
    internal class ProductTemplateAlphaSizeParametersExtractor : IProductTypeParametersExtractor
    {
        /// <inheritdoc/>
        public Task ExtractParameter<T>(IDictionary<string, string> parameters, T product, string _, CancellationToken cancellationToken)
        {
            if (product is ProductTemplateAlphaSize productTemplateAlphaSize)
            {
                var alphaSizes = productTemplateAlphaSize.ProductVariants.Select(x => x.ProductOptionAlphaSize).Distinct();

                parameters.Add("Sizes", string.Join(", ", alphaSizes));
            }

            return Task.CompletedTask;
        }
    }
}
