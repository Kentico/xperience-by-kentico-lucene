using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

using DancingGoat.Models;

namespace DancingGoat.ViewComponents
{
    /// <summary>
    /// Generates the schema.org WebSite and Organization structured data for the home page.
    /// </summary>
    public sealed class WebSiteJsonLdGenerator : IJsonLdGenerator
    {
        private const string SITE_NAME = "Dancing Goat";
        private const string LOGO_PATH = "/Content/Images/logo.svg";


        /// <inheritdoc/>
        public bool SupportsModel(object viewModel)
        {
            return viewModel is HomePage;
        }


        /// <inheritdoc/>
        public Task<IEnumerable<JsonObject>> Generate(object viewModel, SeoMetadataContext context, CancellationToken cancellationToken)
        {
            var siteUrl = context.BaseUri.ToString();

            var webSiteJsonLd = new JsonObject
            {
                ["@context"] = "https://schema.org",
                ["@type"] = "WebSite",
                ["name"] = SITE_NAME,
                ["url"] = siteUrl
            };

            var organizationJsonLd = new JsonObject
            {
                ["@context"] = "https://schema.org",
                ["@type"] = "Organization",
                ["name"] = SITE_NAME,
                ["url"] = siteUrl,
                ["logo"] = context.GetAbsoluteUrl(LOGO_PATH)
            };

            return Task.FromResult<IEnumerable<JsonObject>>([webSiteJsonLd, organizationJsonLd]);
        }
    }
}
