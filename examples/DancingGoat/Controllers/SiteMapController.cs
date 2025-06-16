using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

using CMS.ContentEngine;
using CMS.Helpers;
using CMS.Websites;
using CMS.Websites.Routing;

using DancingGoat.Models;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DancingGoat.Controllers
{
    /// <summary>
    /// Controller for generating a sitemap.
    /// </summary>
    public class SiteMapController : Controller
    {
        private readonly IContentQueryExecutor contentQueryExecutor;
        private readonly IProgressiveCache progressiveCache;
        private readonly IWebsiteChannelContext websiteChannelContext;
        private static readonly double cacheMinutes = TimeSpan.FromDays(0).TotalMinutes;

        /// <summary>
        /// Initializes a new instance of the <see cref="SiteMapController"/> class.
        /// </summary>
        public SiteMapController(
            IContentQueryExecutor contentQueryExecutor,
            IProgressiveCache progressiveCache,
            IWebsiteChannelContext websiteChannelContext)
        {
            this.contentQueryExecutor = contentQueryExecutor;
            this.progressiveCache = progressiveCache;
            this.websiteChannelContext = websiteChannelContext;
        }


        [HttpGet]
        [Route("/sitemap.xml")]
        public async Task<ContentResult> Index()
        {
            var sitemapXml = await progressiveCache.LoadAsync(async _ => await GenerateSitemapXml(), GetCacheSettings());

            return Content(sitemapXml, MediaTypeNames.Application.Xml);

            CacheSettings GetCacheSettings() => new(cacheMinutes, $"{nameof(SiteMapController)}|{nameof(Index)}")
            {
                GetCacheDependency = () => CacheHelper.GetCacheDependency(
                    [
                        // Since we can't detect only reusable field schema-based page changes,
                        // we need to respond to all changes in the content tree
                        $"webpageitem|bychannel|{websiteChannelContext.WebsiteChannelName}|childrenofpath|/",
                            // Since we can't detect only reusable field schema changes or
                            // additions or removals within a content type definition,
                            // we need to respond to all changes in content types
                            "cms.contenttype|all"
                    ])
            };
        }


        private async Task<string> GenerateSitemapXml()
        {
            var options = new ContentQueryExecutionOptions
            {
                ForPreview = false,
                IncludeSecuredItems = false
            };

            var builder = new ContentItemQueryBuilder()
                // Get all pages with the SEO fields schema in the current website
                .ForContentTypes(p => p.OfReusableSchema(ISEOFields.REUSABLE_FIELD_SCHEMA_NAME).ForWebsite())
                .Parameters(p =>
                    // Limit data to required columns
                    p.UrlPathColumns()
                    // Filter out pages that don't allow search indexing,
                    // the default value is true, so null values are considered as true as well
                    .Where(w =>
                        w.WhereNull(nameof(ISEOFields.SEOFieldsAllowSearchIndexing))
                            .Or().WhereTrue(nameof(ISEOFields.SEOFieldsAllowSearchIndexing))));

            var languagePaths = await contentQueryExecutor.GetMappedWebPageResult<IWebPageFieldsSource>(builder, options, HttpContext.RequestAborted);

            return BuildSitemap(languagePaths, HttpContext.Request);
        }


        private string BuildSitemap(IEnumerable<IWebPageFieldsSource> pages, HttpRequest request)
        {
            var stringBuilder = new StringBuilder();
            using (var xmlWriter = XmlWriter.Create(stringBuilder, new XmlWriterSettings { Indent = true }))
            {
                xmlWriter.WriteStartDocument();
                xmlWriter.WriteStartElement("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");

                foreach (var page in pages)
                {
                    var pageUrl = page.GetUrl();

                    xmlWriter.WriteStartElement("url");
                    xmlWriter.WriteElementString("loc", pageUrl.AbsoluteUrl);
                    xmlWriter.WriteEndElement();
                }

                xmlWriter.WriteEndElement();
                xmlWriter.WriteEndDocument();
            }

            return stringBuilder.ToString();
        }
    }
}
