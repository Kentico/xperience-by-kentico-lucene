using CMS.Core;
using CMS.DocumentEngine;
using CMS.Helpers;
using Kentico.Content.Web.Mvc;
using Microsoft.Net.Http.Headers;

namespace DancingGoat.Search;
public class WebCrawlerService
{
    private readonly HttpClient httpClient;
    private readonly IPageUrlRetriever urlRetriever;
    private readonly IEventLogService eventLogService;

    public WebCrawlerService(HttpClient httpClient, IPageUrlRetriever urlRetriever, IEventLogService eventLogService)
    {
        this.httpClient = httpClient;
        // configure the client inside constructor if needed (add custom headers etc.)
        this.httpClient.DefaultRequestHeaders.Add(HeaderNames.UserAgent, "SearchCrawler");
        // get crawler base url from settings or site configuration, make sure that WebCrawlerBaseUrl is correct
        string baseUrl = ValidationHelper.GetString(Service.Resolve<IAppSettingsService>()["WebCrawlerBaseUrl"], DocumentURLProvider.GetDomainUrl("DancingGoatCore"));
        this.httpClient.BaseAddress = new Uri(baseUrl);

        this.urlRetriever = urlRetriever;
        this.eventLogService = eventLogService;
    }

    public async Task<string> CrawlNode(TreeNode node)
    {
        try
        {
            // use relative path, '/' is stripped to handle base urls which are not domain root
            string url = urlRetriever.Retrieve(node, node.DocumentCulture).RelativePath.TrimStart('~').TrimStart('/');
            // urlRetriever.Retrieve(node).AbsolutePath and no BaseAddress could be used as an alternative
            return await CrawlPage(url);
        }
        catch (Exception ex)
        {
            eventLogService.LogException(nameof(WebCrawlerService), nameof(CrawlNode), ex, 0, $"NodeAliasPath: {node.NodeAliasPath}");
        }
        return "";
    }

    public async Task<string> CrawlPage(string url)
    {
        try
        {
            var response = await httpClient.GetAsync(url);
            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            eventLogService.LogException(nameof(WebCrawlerService), nameof(CrawlPage), ex, 0, $"Url: {url}");
        }
        return "";
    }
}
