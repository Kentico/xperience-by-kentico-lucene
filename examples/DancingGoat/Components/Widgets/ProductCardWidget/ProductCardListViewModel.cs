using System.Collections.Generic;
using System.Linq;

using CMS.ContentEngine;

using DancingGoat.Models;

namespace DancingGoat.Widgets
{
    /// <summary>
    /// View model for Product card widget.
    /// </summary>
    public class ProductCardListViewModel
    {
        /// <summary>
        /// Collection of products.
        /// </summary>
        public IEnumerable<ProductCardViewModel> Products { get; set; }


        /// <summary>
        /// Gets ViewModels for <paramref name="products"/>.
        /// </summary>
        /// <param name="products">Collection of products.</param>
        /// <param name="productPageUrls">Product detail page URLs indexed by content item ID.</param>
        /// <returns>Hydrated ViewModel.</returns>
        public static ProductCardListViewModel GetViewModel(IEnumerable<IProductFields> products, Dictionary<int, string> productPageUrls)
        {
            var productModels = new List<ProductCardViewModel>();

            foreach (var product in products.Where(product => product != null))
            {
                productPageUrls.TryGetValue(((IContentItemFieldsSource)product).SystemFields.ContentItemID, out var url);
                var productModel = ProductCardViewModel.GetViewModel(product, url);
                productModels.Add(productModel);
            }

            return new ProductCardListViewModel
            {
                Products = productModels
            };
        }
    }
}
