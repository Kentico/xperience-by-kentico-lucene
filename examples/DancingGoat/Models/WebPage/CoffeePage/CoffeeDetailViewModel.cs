using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using CMS.ContentEngine;

using Tag = CMS.ContentEngine.Tag;

namespace DancingGoat.Models
{
    public record CoffeeDetailViewModel(string Name, string Description, string ImageUrl, IEnumerable<Tag> Tastes, IEnumerable<Tag> Processing)
    {
        /// <summary>
        /// Maps <see cref="CoffeePage"/> to a <see cref="CoffeeDetailViewModel"/>.
        /// </summary>
        public async static Task<CoffeeDetailViewModel> GetViewModel(CoffeePage coffeePage, string languageName, ITaxonomyRetriever taxonomyRetriever)
        {
            var coffee = coffeePage.RelatedItem.FirstOrDefault();
            var image = coffee.ProductFieldsImage.FirstOrDefault();

            return new CoffeeDetailViewModel(
                coffee.ProductFieldsName,
                coffee.ProductFieldsDescription,
                image?.ImageFile.Url,
                await taxonomyRetriever.RetrieveTags(coffee.CoffeeTastes.Select(taste => taste.Identifier), languageName),
                await taxonomyRetriever.RetrieveTags(coffee.CoffeeProcessing.Select(processing => processing.Identifier), languageName)
            );
        }
    }
}
