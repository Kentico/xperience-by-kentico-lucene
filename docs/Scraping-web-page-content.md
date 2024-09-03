# Scraping web page content

Below is an example of how you can create a web page scraper to index the content of a web page item that might be rendered
using the Page Builder or related content.

> Your `HttpClient.BaseAddress` needs to match the web page item's website channel baseUrl. In this example, the crawler only supports 1 website channel with a base URL stored in `appsettings.json`. You can extend this to pull this value dynamically from the channel settings of the solution. See `DefaultLuceneClient.GetAllWebsiteChannels` for an example query.

## Scraping services

First add your website url base to the `WebCrawlerBaseUrl` `appsettings.json`

```json
{
    // Other settings ...
    // Our example runs on localhost with port: 58093
    "WebCrawlerBaseUrl": "http://localhost:58093/"
}
```

```csharp
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
            var url = await urlRetriever.Retrieve(page);
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
        return string.Empty;
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
        return string.Empty;
    }
}
```

We'll also want to process and sanitize the scraped HTML, removing any irrelavent content and removing all markup.

```csharp
using AngleSharp.Html.Parser;
using CMS.Helpers;

public class WebScraperHtmlSanitizer
{
    public virtual string SanitizeHtmlDocument(string htmlContent)
    {
        if (string.IsNullOrWhiteSpace(htmlContent))
        {
            return string.Empty;
        }

        var parser = new HtmlParser();
        var doc = parser.ParseDocument(htmlContent);
        var body = doc.Body;
        if (body is null)
        {
            return string.Empty;
        }

        foreach (var element in body.QuerySelectorAll("script"))
        {
            element.Remove();
        }

        foreach (var element in body.QuerySelectorAll("style"))
        {
            element.Remove();
        }

        // Removes elements marked with the default Xperience exclusion attribute
        foreach (var element in body.QuerySelectorAll($"*[{"data-ktc-search-exclude"}]"))
        {
            element.Remove();
        }

        foreach (var element in body.QuerySelectorAll("header"))
        {
            element.Remove();
        }

        foreach (var element in body.QuerySelectorAll("footer"))
        {
            element.Remove();
        }

        // Gets the text content of the body element
        string textContent = body.TextContent;

        // Normalizes and trims whitespace characters
        textContent = HTMLHelper.RegexHtmlToTextWhiteSpace.Replace(textContent, " ");
        textContent = textContent.Trim();

        string title = doc.Head?.QuerySelector("title")?.TextContent ?? string.Empty;
        string description = doc.Head?.QuerySelector("meta[name='description']")?.GetAttribute("content") ?? string.Empty;

        return string.Join(
            " ",
            new string[] { title, description, textContent }.Where(i => !string.IsNullOrWhiteSpace(i))
        );
    }
}
```

## Service registration

Register these services in the startup:

```csharp
// Startup.cs or wherever you register services in the DI container

services.AddSingleton<WebScraperHtmlSanitizer>();
services.AddHttpClient<WebCrawlerService>();
```

## Customize the indexing strategy

Now, use the services in your custom strategy to add the scraped content to the index:

```csharp
// ExampleSearchIndexingStrategy.cs

private const string CRAWLER_CONTENT_FIELD_NAME = "Content";

private readonly IWebPageQueryResultMapper webPageMapper;
private readonly IContentQueryExecutor queryExecutor;
private readonly WebScraperHtmlSanitizer htmlSanitizer;
private readonly WebCrawlerService webCrawler;

public ExampleSearchIndexingStrategy(
    IWebPageQueryResultMapper webPageMapper,
    IContentQueryExecutor queryExecutor,
    WebScraperHtmlSanitizer htmlSanitizer,
    WebCrawlerService webCrawler
)
{
    this.webPageMapper = webPageMapper;
    this.queryExecutor = queryExecutor;
    this.htmlSanitizer = htmlSanitizer;
    this.webCrawler = webCrawler;
}

public override async Task<Document?> MapToLuceneDocumentOrNull(IIndexEventItemModel item)
{
    // Setup same as examples in Usage-Guide.md
    // ...

    string content = string.Empty;

    if (item is IndexEventWebPageItemModel webpageItem &&
        string.Equals(indexedModel.ContentTypeName, ArticlePage.CONTENT_TYPE_NAME, StringComparison.OrdinalIgnorecase))
    {
        // ...

        string rawContent = await webCrawler.CrawlWebPage(page);
        content = htmlSanitizer.SanitizeHtmlDocument(rawContent);
    }

    // Add the scraped content
    document.Add(new TextField(CRAWLER_CONTENT_FIELD_NAME, crawlerContent, Field.Store.NO));

    // Set other fields
    // ...

    return document;
}

// ...
```
