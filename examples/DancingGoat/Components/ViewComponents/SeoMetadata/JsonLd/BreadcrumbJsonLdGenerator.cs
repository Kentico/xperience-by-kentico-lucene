using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

using CMS.Websites;

using DancingGoat.Models;

using Kentico.Content.Web.Mvc;

namespace DancingGoat.ViewComponents
{
    /// <summary>
    /// Generates the schema.org BreadcrumbList structured data from the content tree ancestors of the current page.
    /// </summary>
    public sealed class BreadcrumbJsonLdGenerator : IJsonLdGenerator
    {
        private readonly IContentRetriever contentRetriever;


        /// <summary>
        /// Initializes a new instance of the <see cref="BreadcrumbJsonLdGenerator"/> class.
        /// </summary>
        public BreadcrumbJsonLdGenerator(IContentRetriever contentRetriever)
        {
            this.contentRetriever = contentRetriever;
        }


        /// <inheritdoc/>
        public bool SupportsModel(object viewModel)
        {
            return viewModel is IWebPageBasedViewModel { WebPage: not null };
        }


        /// <inheritdoc/>
        public async Task<IEnumerable<JsonObject>> Generate(object viewModel, SeoMetadataContext context, CancellationToken cancellationToken)
        {
            var treePath = ((IWebPageBasedViewModel)viewModel).WebPage.SystemFields.WebPageItemTreePath;
            var ancestorPaths = GetAncestorPaths(treePath);
            if (ancestorPaths.Count == 0)
            {
                return [];
            }

            var ancestors = await contentRetriever.RetrieveAllPages<IWebPageFieldsSource>(
                new RetrieveAllPagesParameters
                {
                    IncludeContentTypeFields = false
                },
                query => query.Where(where => where.WhereIn(nameof(IWebPageContentQueryDataContainer.WebPageItemTreePath), ancestorPaths)),
                new RetrievalCacheSettings($"Breadcrumb_{treePath}"),
                cancellationToken);

            var breadcrumbElements = ancestors
                .OrderBy(ancestor => ancestor.SystemFields.WebPageItemTreePath.Length)
                .Select(ancestor => new BreadcrumbElement(GetPageName(ancestor), ancestor.GetUrl().AbsoluteUrl))
                .Append(new BreadcrumbElement(context.Title, context.CanonicalUrl))
                .Select((element, index) => new JsonObject
                {
                    ["@type"] = "ListItem",
                    ["position"] = index + 1,
                    ["name"] = element.Name,
                    ["item"] = element.Url
                });

            var breadcrumbJsonLd = new JsonObject
            {
                ["@context"] = "https://schema.org",
                ["@type"] = "BreadcrumbList",
                ["itemListElement"] = new JsonArray([.. breadcrumbElements])
            };

            return [breadcrumbJsonLd];
        }


        private static List<string> GetAncestorPaths(string treePath)
        {
            var ancestorPaths = new List<string>();
            var segments = treePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var currentPath = string.Empty;

            foreach (var segment in segments.SkipLast(1))
            {
                currentPath = $"{currentPath}/{segment}";
                ancestorPaths.Add(currentPath);
            }

            return ancestorPaths;
        }


        private static string GetPageName(IWebPageFieldsSource page)
        {
            if (page is ISEOFields seoFields && !string.IsNullOrEmpty(seoFields.SEOFieldsTitle))
            {
                return seoFields.SEOFieldsTitle;
            }

            return page.SystemFields.WebPageItemName;
        }


        private sealed record BreadcrumbElement(string Name, string Url);
    }
}
