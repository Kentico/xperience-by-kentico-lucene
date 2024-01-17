using CMS.Core;
using CMS.Websites;
using Microsoft.Net.Http.Headers;

namespace DancingGoat.Search.Services;

public class WebCrawlerService
{
    private readonly HttpClient httpClient;
    private readonly IEventLogService log;
    private readonly IWebPageUrlRetriever webPageUrlRetriever;

    public WebCrawlerService(
        HttpClient httpClient,
        IEventLogService log,
        IWebPageUrlRetriever webPageUrlRetriever,
        IAppSettingsService appSettingsService)
    {
        string baseUrl = appSettingsService["WebCrawlerBaseUrl"];

        this.httpClient = httpClient;
        this.httpClient.DefaultRequestHeaders.Add(HeaderNames.UserAgent, "SearchCrawler");
        this.httpClient.BaseAddress = new Uri(baseUrl);

        this.log = log;
        this.webPageUrlRetriever = webPageUrlRetriever;
    }

    public async Task<string> CrawlWebPage(IWebPageFieldsSource page)
    {
        try
        {
            var url = await webPageUrlRetriever.Retrieve(page);
            string path = url.RelativePath.TrimStart('~').TrimStart('/');

            return await CrawlPage(path);
        }
        catch (Exception ex)
        {
            log.LogException(
                nameof(WebCrawlerService),
                nameof(CrawlWebPage),
                ex,
                $"Tree Path: {page.SystemFields.WebPageItemTreePath}");
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
            log.LogException(
                nameof(WebCrawlerService),
                nameof(CrawlPage),
                ex,
                $"Url: {url}");
        }
        return "";
    }
}
