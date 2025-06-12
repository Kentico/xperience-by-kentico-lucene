using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CMS.ContentEngine;

using DancingGoat.Models;

namespace DancingGoat.Commerce
{
    /// <inheritdoc cref="IProductTypeParametersExtractor"/>
    public class CoffeeParametersExtractor : IProductTypeParametersExtractor
    {
        private readonly ITaxonomyRetriever taxonomyRetriever;

        public CoffeeParametersExtractor(ITaxonomyRetriever taxonomyRetriever)
        {
            this.taxonomyRetriever = taxonomyRetriever;
        }


        /// <inheritdoc/>
        public async Task ExtractParameter<T>(IDictionary<string, string> parameters, T product, string languageName, CancellationToken cancellationToken)
        {
            if (product is ProductCoffee coffee)
            {
                var identifiers = coffee.CoffeeProcessing.Select(x => x.Identifier)
                    .Union(coffee.CoffeeTastes.Select(x => x.Identifier));
                var taxonomies = await taxonomyRetriever.RetrieveTags(identifiers, languageName, cancellationToken);

                foreach (var taxonomy in taxonomies)
                {
                    var key = coffee.CoffeeProcessing.Any(x => x.Identifier == taxonomy.Identifier)
                        ? "Processing"
                        : "Taste";
                    if (parameters.TryGetValue(key, out string value))
                    {
                        parameters[key] = $"{value}, {taxonomy.Name}";
                    }
                    else
                    {
                        parameters.Add(key, taxonomy.Name);
                    }
                }
            }
        }
    }
}
