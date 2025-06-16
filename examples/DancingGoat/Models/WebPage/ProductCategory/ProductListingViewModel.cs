using System.Collections.Generic;
using System.Linq;

using CMS.ContentEngine;
using CMS.Websites;

namespace DancingGoat.Models
{
    public record ProductListingViewModel(ProductSectionListViewModel SelectionProductListViewModel, IEnumerable<NavigationItemViewModel> CategoryMenuViewModel) : IWebPageBasedViewModel
    {
        /// <inheritdoc/>
        public IWebPageFieldsSource WebPage { get; init; }


        /// <summary>
        /// Validates and maps <see cref="ProductCategory"/> to a <see cref="ProductListingViewModel"/>.
        /// </summary>
        public static ProductListingViewModel GetViewModel(ProductCategory productCategory, IEnumerable<IProductFields> products, IDictionary<int, string> productPageUrls, TaxonomyData productTagsTaxonomy,
            IEnumerable<NavigationItemViewModel> categoryMenu, string languageName)
        {
            if (productCategory == null)
            {
                return null;
            }

            var selection = new ProductSectionListViewModel(null,
                products
                    .Select(product =>
                    {
                        productPageUrls.TryGetValue((product as IContentItemFieldsSource).SystemFields.ContentItemID, out string pageUrl);

                        return ProductListItemViewModel.GetViewModel(
                        product,
                        pageUrl,
                        productTagsTaxonomy.Tags.FirstOrDefault(tag => tag.Identifier == product.ProductFieldTags.FirstOrDefault()?.Identifier)?.Title);
                    })
                    .OrderBy(product => product.Name));

            return new ProductListingViewModel(selection, categoryMenu)
            {
                WebPage = productCategory
            };
        }
    }
}
