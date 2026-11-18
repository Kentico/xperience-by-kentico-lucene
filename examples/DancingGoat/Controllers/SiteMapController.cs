using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Helpers;
using CMS.Websites;

using DancingGoat.Models;

using Kentico.Content.Web.Mvc;

using Microsoft.AspNetCore.Mvc;

namespace DancingGoat.Controllers
{
    /// <summary>
    /// Controller for generating a sitemap.
    /// </summary>
    public class SiteMapController : Controller
    {
        private readonly IContentRetriever contentRetriever;
        private readonly IInfoProvider<ContentLanguageInfo> contentLanguageInfoProvider;
        private readonly IProgressiveCache progressiveCache;

        /// <summary>
        /// Initializes a new instance of the <see cref="SiteMapController"/> class.
        /// </summary>
        public SiteMapController(
            IContentRetriever contentRetriever,
            IInfoProvider<ContentLanguageInfo> contentLanguageInfoProvider,
            IProgressiveCache progressiveCache)
        {
            this.contentRetriever = contentRetriever;
            this.contentLanguageInfoProvider = contentLanguageInfoProvider;
            this.progressiveCache = progressiveCache;
        }


        [HttpGet]
        [Route("/sitemap.xml")]
        public async Task<ContentResult> Index()
        {
            var pages = await GetPages();

            return Content(BuildSitemap(pages), MediaTypeNames.Application.Xml);
        }


        private async Task<List<IWebPageFieldsSource>> GetPages()
        {
            var languageNames = await GetLanguageNames();

            var pages = new List<IWebPageFieldsSource>();

            // Sequential retrieval per language; parallel queries could share a DB connection (MARS risk)
            foreach (var languageName in languageNames)
            {
                var languagePages = await contentRetriever.RetrievePagesOfReusableSchemas<IWebPageFieldsSource>(
                    [ISEOFields.REUSABLE_FIELD_SCHEMA_NAME],
                    new RetrievePagesOfReusableSchemasParameters
                    {
                        LanguageName = languageName,
                        UseLanguageFallbacks = false,
                        IncludeContentTypeFields = false,
                        IsForPreview = false,
                        IncludeSecuredItems = false
                    },
                    query => query.Where(where =>
                        where.WhereFalse(nameof(ISEOFields.SEOFieldsNoIndex))
                            .Or().WhereNull(nameof(ISEOFields.SEOFieldsNoIndex))),
                    new RetrievalCacheSettings($"NotNoIndex_{nameof(ISEOFields.SEOFieldsNoIndex)}"),
                    HttpContext.RequestAborted);

                pages.AddRange(languagePages);
            }

            return pages;
        }


        private async Task<IEnumerable<string>> GetLanguageNames()
        {
            return await progressiveCache.LoadAsync(async (cacheSettings, cancellationToken) =>
            {
                cacheSettings.CacheDependency = CacheHelper.GetCacheDependency($"{ContentLanguageInfo.OBJECT_TYPE}|all");

                return (await contentLanguageInfoProvider.Get()
                    .Column(nameof(ContentLanguageInfo.ContentLanguageName))
                    .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken))
                    .Select(language => language.ContentLanguageName)
                    .ToList();
            },
            new CacheSettings(10, nameof(SiteMapController), nameof(GetLanguageNames)), HttpContext.RequestAborted);
        }


        private static string BuildSitemap(IEnumerable<IWebPageFieldsSource> pages)
        {
            var stringBuilder = new StringBuilder();
            using (var xmlWriter = XmlWriter.Create(stringBuilder, new XmlWriterSettings { Indent = true }))
            {
                xmlWriter.WriteStartDocument();
                xmlWriter.WriteStartElement("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");

                foreach (var page in pages)
                {
                    var pageUrl = page.GetUrl();
                    var lastModified = page.SystemFields.ContentItemCommonDataLastPublishedWhen;

                    xmlWriter.WriteStartElement("url");
                    xmlWriter.WriteElementString("loc", pageUrl.AbsoluteUrl);
                    if (lastModified.HasValue)
                    {
                        xmlWriter.WriteElementString("lastmod", lastModified.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                    }
                    xmlWriter.WriteEndElement();
                }

                xmlWriter.WriteEndElement();
                xmlWriter.WriteEndDocument();
            }

            return stringBuilder.ToString();
        }
    }
}
