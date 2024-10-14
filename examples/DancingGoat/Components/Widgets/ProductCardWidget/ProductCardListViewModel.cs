using System.Collections.Generic;
using System.Linq;

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
        /// <returns>Hydrated ViewModel.</returns>
        public static ProductCardListViewModel GetViewModel(IEnumerable<IProductFields> products)
        {
            var productModels = new List<ProductCardViewModel>();

            foreach (var product in products.Where(product => product != null))
            {
                var productModel = ProductCardViewModel.GetViewModel(product);
                productModels.Add(productModel);
            }

            return new ProductCardListViewModel
            {
                Products = productModels
            };
        }
    }
}
