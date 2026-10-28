using System.Collections.Generic;
using System.Globalization;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

using CMS.Helpers;

using DancingGoat.Commerce;
using DancingGoat.Models;

namespace DancingGoat.ViewComponents
{
    /// <summary>
    /// Generates the schema.org Product structured data with an Offer for product detail pages.
    /// </summary>
    public sealed class ProductJsonLdGenerator : IJsonLdGenerator
    {
        /// <inheritdoc/>
        public bool SupportsModel(object viewModel)
        {
            return viewModel is ProductViewModel;
        }


        /// <inheritdoc/>
        public Task<IEnumerable<JsonObject>> Generate(object viewModel, SeoMetadataContext context, CancellationToken cancellationToken)
        {
            var product = (ProductViewModel)viewModel;

            var offer = new JsonObject
            {
                ["@type"] = "Offer",
                ["price"] = product.Price.ToString("0.00", CultureInfo.InvariantCulture),
                ["priceCurrency"] = PriceFormatter.CurrencyCode,
                ["availability"] = "https://schema.org/InStock"
            };

            if (!string.IsNullOrEmpty(context.CanonicalUrl))
            {
                offer["url"] = context.CanonicalUrl;
            }

            var productJsonLd = new JsonObject
            {
                ["@context"] = "https://schema.org",
                ["@type"] = "Product",
                ["name"] = product.Name,
                ["offers"] = offer
            };

            if (!string.IsNullOrEmpty(product.Description))
            {
                productJsonLd["description"] = HTMLHelper.StripTags(product.Description);
            }

            var imageUrl = context.GetAbsoluteUrl(product.ImagePath);
            if (!string.IsNullOrEmpty(imageUrl))
            {
                productJsonLd["image"] = imageUrl;
            }

            return Task.FromResult<IEnumerable<JsonObject>>([productJsonLd]);
        }
    }
}
