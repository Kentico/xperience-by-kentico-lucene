using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using CMS.Websites;

namespace DancingGoat.Models
{
    public record ProductListItemViewModel(string Name, string ImagePath, string Url)
    {
        public static async Task<ProductListItemViewModel> GetViewModel(ProductPage productPage, IWebPageUrlRetriever urlRetriever, string languageName)
        {
            var product = productPage.RelatedItem.FirstOrDefault();
            var image = product.Image.FirstOrDefault();

            var path = (await urlRetriever.Retrieve(productPage, languageName)).RelativePath;

            return new ProductListItemViewModel(
                product.Name,
                image?.ImageFile.Url,
                path
            );
        }
    }
}