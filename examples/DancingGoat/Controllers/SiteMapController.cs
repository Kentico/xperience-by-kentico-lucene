using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Websites;

using DancingGoat.Models;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace DancingGoat.Controllers
{
    /// <summary>
    /// Controller for generating a sitemap.
    /// </summary>
    public class SiteMapController : Controller
    {
        private const string XML_TYPE = "application/xml";

        private readonly IContentQueryExecutor contentQueryExecutor;
        private readonly IWebPageUrlRetriever urlRetriever;
        private readonly IInfoProvider<ContentLanguageInfo> contentLanguageProvider;


        /// <summary>
        /// Initializes a new instance of the <see cref="SiteMapController"/> class.
        /// </summary>
        public SiteMapController(IContentQueryExecutor contentQueryExecutor, IWebPageUrlRetriever urlRetriever, IInfoProvider<ContentLanguageInfo> contentLanguageProvider)
        {
            this.contentQueryExecutor = contentQueryExecutor;
            this.urlRetriever = urlRetriever;
            this.contentLanguageProvider = contentLanguageProvider;
        }


        [HttpGet]
        [Route("/sitemap.xml")]
        public async Task<ContentResult> Index()
        {
            var options = new ContentQueryExecutionOptions
            {
                ForPreview = false,
                IncludeSecuredItems = false
            };

            var relativeUrls = new List<string>();

            foreach (var language in contentLanguageProvider.Get().OrderByDescending(i => i.ContentLanguageIsDefault))
            {
                var builder = new ContentItemQueryBuilder().ForContentTypes(p => p.OfReusableSchema("SEOFields").ForWebsite())
                                                           .InLanguage(language.ContentLanguageName, false)
                                                           .Parameters(p => p.Columns(nameof(IWebPageContentQueryDataContainer.WebPageItemID))
                                                                             .Where(w => w.WhereTrue(nameof(ISEOFields.SEOFieldsAllowSearchIndexing))));

                var pageIdentifiers = await contentQueryExecutor.GetWebPageResult(builder, i => i.WebPageItemID, options, HttpContext.RequestAborted);
                var languageUrls = await GetWebPageRelativeUrls(pageIdentifiers, language.ContentLanguageName);

                relativeUrls.AddRange(languageUrls);
            }

            var absoluteUrls = GetAbsoluteUrls(relativeUrls);
            var document = GetSitemap(absoluteUrls);

            return Content(document.OuterXml, XML_TYPE);
        }


        private async Task<IEnumerable<string>> GetWebPageRelativeUrls(IEnumerable<int> pageIdentifiers, string languageName)
        {
            var relativeUrls = new List<string>();

            foreach (var pageIdentifier in pageIdentifiers)
            {
                var webPageUrl = await urlRetriever.Retrieve(pageIdentifier, languageName, false, HttpContext.RequestAborted);
                relativeUrls.Add(webPageUrl.RelativePath.TrimStart('~'));
            }

            return relativeUrls;
        }


        private IEnumerable<string> GetAbsoluteUrls(IEnumerable<string> relativeUrls)
        {
            var request = HttpContext.Request;

            return relativeUrls.Select(i => UriHelper.BuildAbsolute(request.Scheme, request.Host, path: i)).OrderBy(i => i);
        }


        private static XmlDocument GetSitemap(IEnumerable<string> urls)
        {
            var document = new XmlDocument();

            var urlSet = document.CreateElement("urlset");
            urlSet.SetAttribute("xmlns", "http://www.sitemaps.org/schemas/sitemap/0.9");

            foreach (var url in urls)
            {
                var element = document.CreateElement("url");
                var location = document.CreateElement("loc");
                location.InnerText = url;

                element.AppendChild(location);
                urlSet.AppendChild(element);
            }

            document.AppendChild(urlSet);

            return document;
        }
    }
}
