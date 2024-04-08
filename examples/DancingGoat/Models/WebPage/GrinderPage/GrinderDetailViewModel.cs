using CMS.ContentEngine;

namespace DancingGoat.Models;

public record GrinderDetailViewModel(string Name, string Description, string ImageUrl, IEnumerable<CMS.ContentEngine.Tag> Manufacturers, IEnumerable<CMS.ContentEngine.Tag> Type)
{
    /// <summary>
    /// Maps <see cref="GrinderPage"/> to a <see cref="GrinderDetailViewModel"/>.
    /// </summary>
    public async static Task<GrinderDetailViewModel> GetViewModel(GrinderPage grinderPage, string languageName, ITaxonomyRetriever taxonomyRetriever)
    {
        var grinder = grinderPage.RelatedItem.FirstOrDefault();
        var image = grinder.Image.FirstOrDefault();

        return new GrinderDetailViewModel(
            grinder.Name,
            grinder.Description,
            image?.ImageFile.Url,
            await taxonomyRetriever.RetrieveTags(grinder.GrinderManufacturer.Select(manufacturer => manufacturer.Identifier), languageName),
            await taxonomyRetriever.RetrieveTags(grinder.GrinderType.Select(type => type.Identifier), languageName)
        );
    }
}
