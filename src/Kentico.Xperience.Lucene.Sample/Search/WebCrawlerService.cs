using CMS.DocumentEngine;
using Kentico.Content.Web.Mvc;
using Microsoft.Net.Http.Headers;

namespace DancingGoat.Search;

public class WebCrawlerService
{
    private readonly HttpClient httpClient;
    private readonly IPageUrlRetriever urlRetriever;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S125:Sections of code should not be commented out", Justification = "Comments contain possible alternative solutions")]
    public WebCrawlerService(HttpClient httpClient, IPageUrlRetriever urlRetriever)
    {
        this.httpClient = httpClient;
        // configure the client inside constructor if needed (add custom headers etc.)
        this.httpClient.DefaultRequestHeaders.Add(HeaderNames.UserAgent, "SearchCrawler");
        this.httpClient.BaseAddress = new Uri(DocumentURLProvider.GetDomainUrl("DancingGoatCore"));
        // alternatively specify custom url or load it from settings
        // this.httpClient.BaseAddress = new Uri("http://localhost:41489/");

        this.urlRetriever = urlRetriever;
    }

    public async Task<string> CrawlNode(TreeNode node)
    {
        string url = urlRetriever.Retrieve(node).RelativePath.TrimStart('~');
        // urlRetriever.Retrieve(node).AbsolutePath and no BaseAddress could be used as an alternative
        return await CrawlPage(url);
    }

    public async Task<string> CrawlPage(string url)
    {
        var response = await httpClient.GetAsync(url);
        return await response.Content.ReadAsStringAsync();
    }
}
