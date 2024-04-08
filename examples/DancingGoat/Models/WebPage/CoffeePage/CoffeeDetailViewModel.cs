using CMS.ContentEngine;

namespace DancingGoat.Models;

public record CoffeeDetailViewModel(string Name, string Description, string ImageUrl, IEnumerable<CMS.ContentEngine.Tag> Tastes, IEnumerable<CMS.ContentEngine.Tag> Processing)
{
    /// <summary>
    /// Maps <see cref="CoffeePage"/> to a <see cref="CoffeeDetailViewModel"/>.
    /// </summary>
    public async static Task<CoffeeDetailViewModel> GetViewModel(CoffeePage coffeePage, string languageName, ITaxonomyRetriever taxonomyRetriever)
    {
        var coffee = coffeePage.RelatedItem.FirstOrDefault();
        var image = coffee.Image.FirstOrDefault();

        return new CoffeeDetailViewModel(
            coffee.Name,
            coffee.Description,
            image?.ImageFile.Url,
            await taxonomyRetriever.RetrieveTags(coffee.CoffeeTastes.Select(taste => taste.Identifier), languageName),
            await taxonomyRetriever.RetrieveTags(coffee.CoffeeProcessing.Select(processing => processing.Identifier), languageName)
        );
    }
}
