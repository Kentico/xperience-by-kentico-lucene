using System.Linq;

using CMS.ContentEngine;

using DancingGoat.Commerce;

namespace DancingGoat.Models
{
    /// <summary>
    /// Represents the tag chip displayed on a product card.
    /// </summary>
    public record ProductListItemTagViewModel(string Title)
    {
        /// <summary>
        /// Maps the first tag of a product to a <see cref="ProductListItemTagViewModel"/>.
        /// </summary>
        /// <param name="product">Product whose tag is displayed.</param>
        /// <param name="productTagsTaxonomy">"Product tags" taxonomy data.</param>
        /// <returns>The tag chip view model, or <c>null</c> when the product has no displayable tag.</returns>
        public static ProductListItemTagViewModel GetViewModel(IProductFields product, TaxonomyData productTagsTaxonomy)
        {
            var tagIdentifier = product.ProductFieldTags.FirstOrDefault()?.Identifier;
            if (tagIdentifier == null)
            {
                return null;
            }

            var tag = productTagsTaxonomy.Tags.FirstOrDefault(t => t.Identifier == tagIdentifier);

            return GetViewModel(tag?.Title);
        }


        /// <summary>
        /// Maps a taxonomy tag to a <see cref="ProductListItemTagViewModel"/>.
        /// </summary>
        /// <param name="tagTitle">Display title of the tag.</param>
        /// <returns>The tag chip view model, or <c>null</c> when there is no title to display.</returns>
        public static ProductListItemTagViewModel GetViewModel(string tagTitle)
        {
            if (string.IsNullOrEmpty(tagTitle))
            {
                return null;
            }

            return new ProductListItemTagViewModel(tagTitle);
        }
    }
}
