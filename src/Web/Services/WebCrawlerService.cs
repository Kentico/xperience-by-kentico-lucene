using System.Net.Http;
using System.Threading.Tasks;
using System;
using Microsoft.Net.Http.Headers;
using CMS.Core;
using CMS.Helpers;
using CMS.Websites;
using Kentico.Content.Web.Mvc;

namespace DancingGoat
{
    public class WebCrawlerService
    {
        private readonly HttpClient httpClient;
        private readonly IEventLogService eventLogService;
        private readonly IWebPageUrlRetriever webPageUrlRetriever;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S125:Sections of code should not be commented out", Justification = "Comments contain possible alternative solutions")]
        public WebCrawlerService(HttpClient httpClient,
            IEventLogService eventLogService,
            IWebPageUrlRetriever webPageUrlRetriever)
        {
            this.httpClient = httpClient;
            this.httpClient.DefaultRequestHeaders.Add(HeaderNames.UserAgent, "SearchCrawler");
            string baseUrl = ValidationHelper.GetString(Service.Resolve<IAppSettingsService>()["WebCrawlerBaseUrl"], "");
            this.httpClient.BaseAddress = new Uri(baseUrl);
            this.eventLogService = eventLogService;
            this.webPageUrlRetriever = webPageUrlRetriever; 
        }

        public async Task<string> CrawlNode(IWebPageContentQueryDataContainer container, string languageName)
        {
            try
            {
                var url = (await webPageUrlRetriever.Retrieve(container.WebPageItemID, languageName)).RelativePath.TrimStart('~');
                return await CrawlPage(url);
            }
            catch (Exception ex)
            {
                eventLogService.LogException(nameof(WebCrawlerService), nameof(CrawlNode), ex, $"WebPageItemTreePath: {container.WebPageItemTreePath}");
            }
            return "";
        }

        public async Task<string> CrawlPage(string url)
        {
            try { 
                var response = await httpClient.GetAsync(url);
                return await response.Content.ReadAsStringAsync();
            } catch (Exception ex)
            {
                eventLogService.LogException(nameof(WebCrawlerService), nameof(CrawlPage), ex, $"Url: {url}");
            }
            return "";
        }


    }
}
