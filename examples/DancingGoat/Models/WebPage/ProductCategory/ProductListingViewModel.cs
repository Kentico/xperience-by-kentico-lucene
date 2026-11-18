using System.Collections.Generic;
using System.Linq;

using CMS.ContentEngine;
using CMS.Websites;

using DancingGoat.Commerce;

namespace DancingGoat.Models
{
    public record ProductListingViewModel(ProductSectionListViewModel SelectionProductListViewModel, IEnumerable<NavigationItemViewModel> CategoryMenuViewModel, string CategoryName) : IWebPageBasedViewModel
    {
        /// <inheritdoc/>
        public IWebPageFieldsSource WebPage { get; init; }


        /// <summary>
        /// Validates and maps <see cref="ProductCategory"/> to a <see cref="ProductListingViewModel"/>.
        /// </summary>
        public static ProductListingViewModel GetViewModel(ProductCategory productCategory, IEnumerable<IProductFields> products, IEnumerable<DancingGoatPriceCalculationResultItem> calculationResultItems, IDictionary<int, string> productPageUrls, TaxonomyData productTagsTaxonomy,
            IEnumerable<NavigationItemViewModel> categoryMenu, string languageName, ISet<int> productIdsWithVariants, TaxonomyData productCategoriesTaxonomy, ISet<int> freeShippingProductIds, ISet<int> specialOfferProductIds)
        {
            if (productCategory == null)
            {
                return null;
            }

            var categoryName = string.Join(", ", productCategory.ProductCategoryTag
                .Select(reference => productCategoriesTaxonomy.Tags.FirstOrDefault(tag => tag.Identifier == reference.Identifier)?.Title)
                .Where(title => !string.IsNullOrEmpty(title)));

            var selection = new ProductSectionListViewModel(null,
                products
                    .Where(product => productPageUrls.ContainsKey((product as IContentItemFieldsSource).SystemFields.ContentItemID))
                    .Select(product =>
                    {
                        productPageUrls.TryGetValue((product as IContentItemFieldsSource).SystemFields.ContentItemID, out string pageUrl);

                        var productCalculationItem = calculationResultItems.FirstOrDefault(item => item.ProductIdentifier.Identifier == (product as IContentItemFieldsSource).SystemFields.ContentItemID);

                        return ProductListItemViewModel.GetViewModel(
                            product,
                            productCalculationItem,
                            pageUrl,
                            ProductListItemTagViewModel.GetViewModel(product, productTagsTaxonomy),
                            productIdsWithVariants.Contains((product as IContentItemFieldsSource).SystemFields.ContentItemID),
                            freeShippingProductIds.Contains((product as IContentItemFieldsSource).SystemFields.ContentItemID),
                            specialOfferProductIds.Contains((product as IContentItemFieldsSource).SystemFields.ContentItemID));
                    })
                    .OrderBy(product => product.Name));

            return new ProductListingViewModel(selection, categoryMenu, categoryName)
            {
                WebPage = productCategory
            };
        }
    }
}
