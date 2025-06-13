using System;
using System.Collections.Generic;
using System.Linq;

using CMS.ContentEngine;
using CMS.Websites;

namespace DancingGoat.Models
{
    public record StoreViewModel(IEnumerable<ProductSectionListViewModel> SelectionProductList, IEnumerable<NavigationItemViewModel> CategoryMenuViewModel) : IWebPageBasedViewModel
    {
        /// <inheritdoc/>
        public IWebPageFieldsSource WebPage { get; init; }


        /// <summary>
        /// Validates and maps <see cref="Store"/> to a <see cref="StoreViewModel"/>.
        /// </summary>
        /// <param name="store">Store page.</param>
        /// <param name="products">Products to be displayed.</param>
        /// <param name="productPageUrls">Product page URLs.</param>
        /// <param name="productSectionTagNames">Tag names that define separate sets of product to be displayed.</param>
        /// <param name="productTagsTaxonomy">"Product tags" taxonomy data</param>
        /// <param name="languageName">Language name to map.</param>
        /// <param name="categoryMenuViewModel">Category menu view model to map.</param>
        public static StoreViewModel GetViewModel(Store store, IEnumerable<IProductFields> products, IDictionary<int, string> productPageUrls, IEnumerable<string> productSectionTagNames, TaxonomyData productTagsTaxonomy, string languageName, IEnumerable<NavigationItemViewModel> categoryMenuViewModel)
        {
            var productSections = new List<ProductSectionListViewModel>();

            var productSectionTags = productTagsTaxonomy.Tags
                .Where(t => productSectionTagNames.Contains(t.Name, StringComparer.InvariantCultureIgnoreCase))
                .OrderBy(t => productSectionTagNames.ToList().IndexOf(t.Name));

            foreach (var productSectionTag in productSectionTags)
            {
                productSections.Add(new ProductSectionListViewModel(
                    productSectionTag.Title,
                    products
                        .Where(product => product.ProductFieldTags.Any(t => t.Identifier == productSectionTag.Identifier))
                        .Select(product =>
                        {
                            productPageUrls.TryGetValue((product as IContentItemFieldsSource).SystemFields.ContentItemID, out var pageUrl);

                            return new ProductListItemViewModel(
                                product.ProductFieldName,
                                product.ProductFieldImage.FirstOrDefault()?.ImageFile.Url,
                                pageUrl,
                                product.ProductFieldPrice,
                                null);
                        })
                ));
            }

            return new StoreViewModel(productSections, categoryMenuViewModel)
            {
                WebPage = store
            };
        }
    }
}
