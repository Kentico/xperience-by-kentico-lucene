using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CMS.ContentEngine;

using DancingGoat.Models;

namespace DancingGoat.Commerce
{
    /// <inheritdoc cref="IProductTypeParametersExtractor"/>
    internal class ProductManufacturerExtractor : IProductTypeParametersExtractor
    {
        private readonly ITaxonomyRetriever taxonomyRetriever;


        public ProductManufacturerExtractor(ITaxonomyRetriever taxonomyRetriever)
        {
            this.taxonomyRetriever = taxonomyRetriever;
        }


        /// <inheritdoc/>
        public async Task ExtractParameter<T>(IDictionary<string, string> parameters, T product, string languageName, CancellationToken cancellationToken)
        {
            if (product is IProductManufacturer productManufacturer && productManufacturer.ProductManufacturerTag != null)
            {
                var manufacturerTags = await taxonomyRetriever.RetrieveTags(productManufacturer.ProductManufacturerTag.Select(x => x.Identifier), languageName, cancellationToken);
                if (manufacturerTags.Any())
                {
                    parameters.Add("Manufacturer", string.Join(", ", manufacturerTags.Select(x => x.Title)));
                }
            }
        }
    }
}
