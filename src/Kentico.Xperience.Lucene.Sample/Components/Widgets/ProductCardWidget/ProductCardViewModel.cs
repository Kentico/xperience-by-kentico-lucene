using System.Linq;

using CMS.DocumentEngine.Types.DancingGoatCore;

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
        public static ProductCardViewModel GetViewModel(Coffee product)
        {
            if (product == null)
            {
                return null;
            }

            return new ProductCardViewModel
                {
                    Heading = product.DocumentName,
                    ImagePath = (product.Fields.Image.FirstOrDefault() as Media)?.Fields.File?.Url,
                    Text = product.CoffeeShortDescription
                };
        }
    }
}