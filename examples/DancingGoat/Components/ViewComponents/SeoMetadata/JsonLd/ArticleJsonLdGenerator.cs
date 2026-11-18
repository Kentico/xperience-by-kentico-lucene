using System.Collections.Generic;
using System.Globalization;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

using DancingGoat.Models;

namespace DancingGoat.ViewComponents
{
    /// <summary>
    /// Generates the schema.org Article structured data for article detail pages.
    /// </summary>
    public sealed class ArticleJsonLdGenerator : IJsonLdGenerator
    {
        /// <inheritdoc/>
        public bool SupportsModel(object viewModel)
        {
            return viewModel is ArticleDetailViewModel;
        }


        /// <inheritdoc/>
        public Task<IEnumerable<JsonObject>> Generate(object viewModel, SeoMetadataContext context, CancellationToken cancellationToken)
        {
            var article = (ArticleDetailViewModel)viewModel;

            var articleJsonLd = new JsonObject
            {
                ["@context"] = "https://schema.org",
                ["@type"] = "Article",
                ["headline"] = article.Title,
                ["datePublished"] = article.PublicationDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            };

            var lastPublished = article.WebPage?.SystemFields.ContentItemCommonDataLastPublishedWhen;
            if (lastPublished.HasValue)
            {
                articleJsonLd["dateModified"] = lastPublished.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }

            if (!string.IsNullOrEmpty(article.Summary))
            {
                articleJsonLd["description"] = article.Summary;
            }

            var imageUrl = context.GetAbsoluteUrl(article.TeaserUrl);
            if (!string.IsNullOrEmpty(imageUrl))
            {
                articleJsonLd["image"] = imageUrl;
            }

            if (!string.IsNullOrEmpty(context.CanonicalUrl))
            {
                articleJsonLd["mainEntityOfPage"] = context.CanonicalUrl;
            }

            return Task.FromResult<IEnumerable<JsonObject>>([articleJsonLd]);
        }
    }
}
