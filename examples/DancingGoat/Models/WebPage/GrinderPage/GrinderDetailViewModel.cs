using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using CMS.ContentEngine;

using Tag = CMS.ContentEngine.Tag;

namespace DancingGoat.Models
{
    public record GrinderDetailViewModel(string Name, string Description, string ImageUrl, IEnumerable<Tag> Manufacturers, IEnumerable<Tag> Type)
    {
        /// <summary>
        /// Maps <see cref="GrinderPage"/> to a <see cref="GrinderDetailViewModel"/>.
        /// </summary>
        public async static Task<GrinderDetailViewModel> GetViewModel(GrinderPage grinderPage, string languageName, ITaxonomyRetriever taxonomyRetriever)
        {
            var grinder = grinderPage.RelatedItem.FirstOrDefault();
            var image = grinder.ProductFieldsImage.FirstOrDefault();

            return new GrinderDetailViewModel(
                grinder.ProductFieldsName,
                grinder.ProductFieldsDescription,
                image?.ImageFile.Url,
                await taxonomyRetriever.RetrieveTags(grinder.GrinderManufacturer.Select(manufacturer => manufacturer.Identifier), languageName),
                await taxonomyRetriever.RetrieveTags(grinder.GrinderType.Select(type => type.Identifier), languageName)
            );
        }
    }
}
