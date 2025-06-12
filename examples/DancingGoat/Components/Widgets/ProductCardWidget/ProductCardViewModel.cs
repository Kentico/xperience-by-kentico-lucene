using System.Linq;

using DancingGoat.Models;

namespace DancingGoat.Widgets
{
    /// <summary>
    /// View model for Product card widget.
    /// </summary>
    public class ProductCardViewModel
    {
        /// <summary>
        /// Card heading.
        /// </summary>
        public string Heading { get; set; }


        /// <summary>
        /// Card background image path.
        /// </summary>
        public string ImagePath { get; set; }


        /// <summary>
        /// Card text.
        /// </summary>
        public string Text { get; set; }


        /// <summary>
        /// Gets ViewModel for <paramref name="product"/>.
        /// </summary>
        /// <param name="product">Product.</param>
        /// <returns>Hydrated ViewModel.</returns>
        public static ProductCardViewModel GetViewModel(IProductFields product)
        {
            if (product == null)
            {
                return null;
            }

            return new ProductCardViewModel
            {
                Heading = product.ProductFieldName,
                ImagePath = product.ProductFieldImage.FirstOrDefault()?.ImageFile.Url,
                Text = product.ProductFieldDescription
            };
        }
    }
}
