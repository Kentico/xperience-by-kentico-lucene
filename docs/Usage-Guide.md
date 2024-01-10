# Usage Guide

## Detailed Setup

### Create a custom Indexing Strategy

Define a custom `DefaultLuceneIndexingStrategy` implementation to customize how page or content items are processed for the index.
The method is given a `IndexedItemModel` which is a unique representation of any item used on a web page. Every item specified in the admin ui is rebuilt. In the UI you need to specify one or more language, channel name, indexingStrategy and paths with content types. This strategy than evaluates all web page items specified in the administration.

Let's say we specified `ArticlePage` in the admin ui.
Now we implement how we want to save ArticlePage document in our strategy.

The document is indexed representation of the webpageitem.

You specify what fields should be indexed in the document by adding this to the document. You later retrieve data from document based on your implementation.

```csharp
public class ExampleSearchIndexingStrategy : DefaultLuceneIndexingStrategy
{
    public static string SORTABLE_TITLE_FIELD_NAME = "SortableTitle";

    public override async Task<Document?> MapToLuceneDocumentOrNull(IndexedItemModel indexedModel)
    {
        var document = new Document();

        string sortableTitle = "";
        string title = "";

        if (indexedModel.ClassName == ArticlePage.CONTENT_TYPE_NAME)
        {
            var page = await GetPage<ArticlePage>(indexedModel.WebPageItemGuid, indexedModel.ChannelName, indexedModel.LanguageCode, ArticlePage.CONTENT_TYPE_NAME);
            contentType = "news";

            if (page != default)
            {
                var article = page.ArticlePageArticle.FirstOrDefault();

                if (article == null)
                {
                    return null;
                }
            }
            else
            {
                return null;
            }

            if (page != default)
            {
                var article = page.ArticlePageArticle.FirstOrDefault();

                sortableTitle = title = article?.ArticleTitle ?? "";
            }
        }

        document.Add(new TextField(nameof(GlobalSearchResultModel.Title), title, Field.Store.YES));
        document.Add(new StringField(SORTABLE_TITLE_FIELD_NAME, sortableTitle, Field.Store.YES));

        return document;
    }
}
```

We can also specify a facet dimension. Which is later used in your code if you want to create faceted search.

```csharp
public class ExampleSearchIndexingStrategy : DefaultLuceneIndexingStrategy
{
    public string FacetDimension { get; set; } = "ContentType";
    public static string SORTABLE_TITLE_FIELD_NAME = "SortableTitle";

    public override FacetsConfig FacetsConfigFactory()
    {
        var facetConfig = new FacetsConfig();

        facetConfig.SetMultiValued(FacetDimension, true);

        return facetConfig;
    }

    public override async Task<Document?> MapToLuceneDocumentOrNull(IndexedItemModel indexedModel)
    {
        var document = new Document();

        string sortableTitle = "";
        string title = "";

        if (indexedModel.ClassName == ArticlePage.CONTENT_TYPE_NAME)
        {
            var page = await GetPage<ArticlePage>(indexedModel.WebPageItemGuid, indexedModel.ChannelName, indexedModel.LanguageCode, ArticlePage.CONTENT_TYPE_NAME);

            if (page != default)
            {
                var article = page.ArticlePageArticle.FirstOrDefault();

                if (article == null)
                {
                    return null;
                }
            }
            else
            {
                return null;
            }

            if (page != default)
            {
                var article = page.ArticlePageArticle.FirstOrDefault();

                sortableTitle = title = article?.ArticleTitle ?? "";
            }
        }

        document.Add(new FacetField(FacetDimension, contentType));

        document.Add(new TextField(nameof(GlobalSearchResultModel.Title), title, Field.Store.YES));
        document.Add(new StringField(SORTABLE_TITLE_FIELD_NAME, sortableTitle, Field.Store.YES));

        return document;
    }
}
```

It is up to your implementation how do you want to retrieve information about the page, however article page or any webpageitem could be retrieved using `GetPage<T>` method. Where you specify that you want to retrieve `ArticlePage` item in the provided language on the channel using provided id and content type.

```csharp
public class ExampleSearchIndexingStrategy : DefaultLuceneIndexingStrategy
{
    public string FacetDimension { get; set; } = "ContentType";
    public static string SORTABLE_TITLE_FIELD_NAME = "SortableTitle";

    private async Task<T> GetPage<T>(Guid id, string channelName, string languageName, string contentTypeName) where T : IWebPageFieldsSource, new()
    {
        var mapper = Service.Resolve<IWebPageQueryResultMapper>();
        var executor = Service.Resolve<IContentQueryExecutor>();
        var query = new ContentItemQueryBuilder()
            .ForContentType(contentTypeName,
                config =>
                    config
                        .WithLinkedItems(4)
                        .ForWebsite(channelName, includeUrlPath: true)
                        .Where(where => where.WhereEquals(nameof(IWebPageContentQueryDataContainer.WebPageItemGUID), id))
                        .TopN(1))
            .InLanguage(languageName);
        var result = await executor.GetWebPageResult(query, container => mapper.Map<T>(container), null,
        cancellationToken: default);

        return result.FirstOrDefault();
    }

    public override FacetsConfig FacetsConfigFactory()
    {
        var facetConfig = new FacetsConfig();

        facetConfig.SetMultiValued(FacetDimension, true);

        return facetConfig;
    }

    public override async Task<Document?> MapToLuceneDocumentOrNull(IndexedItemModel indexedModel)
    {
        var document = new Document();

        string sortableTitle = "";
        string title = "";
        string contentType = "";

        if (indexedModel.ClassName == ArticlePage.CONTENT_TYPE_NAME)
        {
            var page = await GetPage<ArticlePage>(indexedModel.WebPageItemGuid, indexedModel.ChannelName, indexedModel.LanguageCode, ArticlePage.CONTENT_TYPE_NAME);
            contentType = "news";

            if (page != default)
            {
                var article = page.ArticlePageArticle.FirstOrDefault();

                if (article == null)
                {
                    return null;
                }
            }
            else
            {
                return null;
            }

            if (page != default)
            {
                var article = page.ArticlePageArticle.FirstOrDefault();

                sortableTitle = title = article?.ArticleTitle ?? "";
            }
        }

        document.Add(new FacetField(FacetDimension, contentType));

        document.Add(new TextField(nameof(GlobalSearchResultModel.Title), title, Field.Store.YES));
        document.Add(new StringField(SORTABLE_TITLE_FIELD_NAME, sortableTitle, Field.Store.YES));
        document.Add(new TextField(nameof(GlobalSearchResultModel.ContentType), contentType, Field.Store.YES));

        return document;
    }
}
```

You can reindex web page items after a change in any reusable content item. We provide a `FindItemsToReindex(IndexedContentItemModel changedItem)` method which returns empty list but can be overriden to return a web page item specified by the consumer of the api. These items are then reindexed in the same way as web page items. Here is an example:

```csharp
public class ExampleSearchIndexingStrategy : DefaultLuceneIndexingStrategy
{
    public string FacetDimension { get; set; } = "ContentType";
    public static string SORTABLE_TITLE_FIELD_NAME = "SortableTitle";

    private async Task<T> GetPage<T>(Guid id, string channelName, string languageName, string contentTypeName) where T : IWebPageFieldsSource, new()
    {
        var mapper = Service.Resolve<IWebPageQueryResultMapper>();
        var executor = Service.Resolve<IContentQueryExecutor>();
        var query = new ContentItemQueryBuilder()
            .ForContentType(contentTypeName,
                config =>
                    config
                        .WithLinkedItems(4)
                        .ForWebsite(channelName, includeUrlPath: true)
                        .Where(where => where.WhereEquals(nameof(IWebPageContentQueryDataContainer.WebPageItemGUID), id))
                        .TopN(1))
            .InLanguage(languageName);
        var result = await executor.GetWebPageResult(query, container => mapper.Map<T>(container), null,
        cancellationToken: default);

        return result.FirstOrDefault();
    }

    public override FacetsConfig FacetsConfigFactory()
    {
        var facetConfig = new FacetsConfig();

        facetConfig.SetMultiValued(FacetDimension, true);

        return facetConfig;
    }

    public override async Task<IEnumerable<IndexedItemModel>> FindItemsToReindex(IndexedContentItemModel changedItem)
    {
        var reindexedItems = new List<IndexedItemModel>();

        if (changedItem.ClassName == Article.CONTENT_TYPE_NAME)
        {
            var mapper = Service.Resolve<IWebPageQueryResultMapper>();
            var executor = Service.Resolve<IContentQueryExecutor>();
            var query = new ContentItemQueryBuilder()
                .ForContentType(ArticlePage.CONTENT_TYPE_NAME,
                    config =>
                        config
                            .WithLinkedItems(4)
                            .ForWebsite(INDEXED_WEBSITECHANNEL_NAME, includeUrlPath: true)
                            .Where(x => x.WhereEquals(nameof(ArticlePage.SystemFields.ContentItemCommonDataVersionStatus), VersionStatus.Published)))
                .InLanguage(changedItem.LanguageCode);

            var result = await executor.GetWebPageResult(query, container => mapper.Map<ArticlePage>(container), null,
            cancellationToken: default);

            foreach (var articlePage in result)
            {
                if (articlePage.ArticlePageArticle.Any(x => x.SystemFields.ContentItemGUID == changedItem.ContentItemGuid) ||
                    articlePage.ArticlePageArticle.IsNullOrEmpty())
                {
                    reindexedItems.Add(new IndexedItemModel
                    {
                        ChannelName = INDEXED_WEBSITECHANNEL_NAME,
                        ClassName = ArticlePage.CONTENT_TYPE_NAME,
                        LanguageCode = changedItem.LanguageCode,
                        WebPageItemGuid = articlePage.SystemFields.WebPageItemGUID,
                        WebPageItemTreePath = articlePage.SystemFields.WebPageItemTreePath
                    });
                }
            }
        }

        return reindexedItems;
    }


    public override async Task<Document?> MapToLuceneDocumentOrNull(IndexedItemModel indexedModel)
    {
        var document = new Document();

        string sortableTitle = "";
        string title = "";
        string contentType = "";

        if (indexedModel.ClassName == ArticlePage.CONTENT_TYPE_NAME)
        {
            var page = await GetPage<ArticlePage>(indexedModel.WebPageItemGuid, indexedModel.ChannelName, indexedModel.LanguageCode, ArticlePage.CONTENT_TYPE_NAME);
            contentType = "news";

            if (page != default)
            {
                var article = page.ArticlePageArticle.FirstOrDefault();

                if (article == null)
                {
                    return null;
                }
            }
            else
            {
                return null;
            }

            if (page != default)
            {
                var article = page.ArticlePageArticle.FirstOrDefault();

                sortableTitle = title = article?.ArticleTitle ?? "";
            }
        }

        document.Add(new FacetField(FacetDimension, contentType));

        document.Add(new TextField(nameof(GlobalSearchResultModel.Title), title, Field.Store.YES));
        document.Add(new StringField(SORTABLE_TITLE_FIELD_NAME, sortableTitle, Field.Store.YES));
        document.Add(new TextField(nameof(GlobalSearchResultModel.ContentType), contentType, Field.Store.YES));

        return document;
    }
}
```

You can also Extend this to index content of the page. This implementation is up to you, however we provide a general example which can be used in any app:

Create a `WebCrawlerService` your baseUrl needs to mathc your site baseUrl. We retrieve this url from the appSettings.json in the

```csharp
tring baseUrl = ValidationHelper.GetString(Service.Resolve<IAppSettingsService>()["WebCrawlerBaseUrl"], "");
```

```csharp
public class WebCrawlerService
{
    private readonly HttpClient httpClient;
    private readonly IEventLogService eventLogService;
    private readonly IWebPageUrlRetriever webPageUrlRetriever;

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

    public async Task<string> CrawlNode(IndexedItemModel itemModel)
    {
        try
        {
            //TODO MilaHlavac: improve url parts concatenation for aplications hosted on non root path
            var url = (await webPageUrlRetriever.Retrieve(itemModel.WebPageItemGuid, itemModel.LanguageCode)).RelativePath.TrimStart('~').TrimStart('/');
            return await CrawlPage(url);
        }
        catch (Exception ex)
        {
            eventLogService.LogException(nameof(WebCrawlerService), nameof(CrawlNode), ex, $"WebPageItemTreePath: {itemModel.WebPageItemTreePath}");
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
            eventLogService.LogException(nameof(WebCrawlerService), nameof(CrawlPage), ex, $"Url: {url}");
        }
        return "";
    }
}
```

Create a sanitizer Service

```csharp

public class WebScraperHtmlSanitizer
{
    public virtual string SanitizeHtmlFragment(string htmlContent)
    {

        var parser = new HtmlParser();
        // null is relevant parameter
        var nodes = parser.ParseFragment(htmlContent, null);

        // Removes script tags
        foreach (var element in nodes.QuerySelectorAll("script"))
        {
            element.Remove();
        }

        // Removes script tags
        foreach (var element in nodes.QuerySelectorAll("style"))
        {
            element.Remove();
        }

        // Removes elements marked with the default Xperience exclusion attribute
        foreach (var element in nodes.QuerySelectorAll($"*[{"data-ktc-search-exclude"}]"))
        {
            element.Remove();
        }

        // Gets the text content of the body element
        string textContent = string.Join(" ", nodes.Select(n => n.TextContent));

        // Normalizes and trims whitespace characters
        textContent = HTMLHelper.RegexHtmlToTextWhiteSpace.Replace(textContent, " ");
        textContent = textContent.Trim();

        return textContent;
    }

    public virtual string SanitizeHtmlDocument(string htmlContent)
    {
        if (!string.IsNullOrWhiteSpace(htmlContent))
        {
            var parser = new HtmlParser();
            var doc = parser.ParseDocument(htmlContent);
            var body = doc.Body;
            if (body != null)
            {

                // Removes script tags
                foreach (var element in body.QuerySelectorAll("script"))
                {
                    element.Remove();
                }

                // Removes script tags
                foreach (var element in body.QuerySelectorAll("style"))
                {
                    element.Remove();
                }

                // Removes elements marked with the default Xperience exclusion attribute
                foreach (var element in body.QuerySelectorAll($"*[{"data-ktc-search-exclude"}]"))
                {
                    element.Remove();
                }

                // Removes header
                foreach (var element in body.QuerySelectorAll("header"))
                {
                    element.Remove();
                }

                // Removes breadcrumbs
                foreach (var element in body.QuerySelectorAll(".breadcrumb"))
                {
                    element.Remove();
                }

                // Removes footer
                foreach (var element in body.QuerySelectorAll("footer"))
                {
                    element.Remove();
                }

                // Gets the text content of the body element
                string textContent = body.TextContent;

                // Normalizes and trims whitespace characters
                textContent = HTMLHelper.RegexHtmlToTextWhiteSpace.Replace(textContent, " ");
                textContent = textContent.Trim();

                var title = doc.Head.QuerySelector("title")?.TextContent;
                var description = doc.Head.QuerySelector("meta[name='description']")?.GetAttribute("content");

                return string.Join(" ",
                    new string[] { title, description, textContent }.Where(i => !string.IsNullOrWhiteSpace(i))
                    );
            }
        }

        return string.Empty;
    }
}

```

Register these services in the startup and retrieve them in your strategy:

```csharp
  services.AddSingleton<WebScraperHtmlSanitizer>();
  services.AddHttpClient<WebCrawlerService>();
```

```csharp
private async Task<string> GetPageContent(IndexedItemModel indexedModel)
{
    var htmlSanitizer = Service.Resolve<WebScraperHtmlSanitizer>();
    var webCrawler = Service.Resolve<WebCrawlerService>();

    string content = await webCrawler.CrawlNode(indexedModel);
    return htmlSanitizer.SanitizeHtmlDocument(content);
}
```

Now you can easily add data to your document to use in the search. In the `MapToLuceneDocumentOrNull` method

```csharp
crawlerContent = await GetPageContent(indexedModel);
// ...
document.Add(new TextField(CRAWLER_CONTENT_FIELD_NAME, crawlerContent, Field.Store.NO));
```

To retrieve the data specify a Service which uses the Lucene `Query`. Example :

This indexing strategy allows you to hook into the indexing process by overriding the following methods of the `DefaultLuceneIndexingStrategy`:

- `MapToLuceneDocumentOrNull`
- `FacetsConfigFactory`
- `FindItemsToReindex(IndexedItemModel changedItem)`
- `FindItemsToReindex(IndexedContentItemModel changedItem)`

`FindItemsToReindex` let you specify whether after a Content item or any Web page item other than which you have specified in the admin ui should also be reindexed.
This does not need to be implemented. Default implementation reindexes PageItem when it is directly changed. However advanced user could specify that a page item should be reindexed after a widget or any other item used in the page item is edited.

You can add as many indexes as you want. Each index can have a different set of fields or store data for different [Content Types](https://docs.xperience.io/xp26/developers-and-admins/development/content-types).

### Rebuild the Search Index

The index will initially be empty until you create or modify some content.

To index all existing content, rebuild the index in Xperience's Administration within the Search application added by this library.

Then, execute a search with a customized Lucene `Query` (like the `MatchAllDocsQuery`) using the ILuceneIndexService.

```csharp
// ...

public class GlobalSearchResultModel
{
    public string Title { get; set; }
    public string ContentType { get; set; }
    public string Url { get; set; }

    public static List<string> PossibleFacets { get; set; } = new List<string> {
    "other",
    "news",
    "product",
    };
}

public class SearchService
{
    private const int PHRASE_SLOP = 3;
    private const int MAX_RESULTS = 1000;

    private readonly ILuceneIndexService luceneIndexService;

    public SearchService(ILuceneIndexService luceneIndexService) => this.luceneIndexService = luceneIndexService;

    public GlobalSearchResultModel GlobalSearch(string indexName, string searchText, int pageSize = 20, int page = 1, string facet = null, string sortBy = null)
    {
        var index = IndexStore.Instance.GetIndex(indexName) ?? throw new Exception($"Index {indexName} was not found!!!");
        pageSize = Math.Max(1, pageSize);
        page = Math.Max(1, page);

        int offset = pageSize * (page - 1);
        int limit = pageSize;

        var analyzer = new StandardAnalyzer(LuceneVersion.LUCENE_48);

        var queryBuilder = new QueryBuilder(analyzer);
        //var queryBuilder = new QueryBuilder(index.Analyzer);

        var query = string.IsNullOrWhiteSpace(searchText)
            ? new MatchAllDocsQuery()
            : GetTermQuery(queryBuilder, searchText);

        var indexingStrategy = new ExampleSearchIndexingStrategy();
        //var chosenFacetCategories = new List<string>();
        var chosenSubFacets = new List<string>();

        var combinedQuery = new BooleanQuery();

        combinedQuery.Add(query, Occur.MUST);

        if (facet != null)
        {
            var drillDownQuery = new DrillDownQuery(indexingStrategy.FacetsConfigFactory());

            string[] subFacets = facet.Split(';', StringSplitOptions.RemoveEmptyEntries);

            foreach (var subFacet in subFacets)
            {
                var categoryAndSpecific = subFacet.Split('-', StringSplitOptions.RemoveEmptyEntries);

                chosenSubFacets.Add(subFacet);

            }

            foreach (var chosen in chosenSubFacets)
            {
                drillDownQuery.Add(indexingStrategy.FacetDimension, chosen);
            }

            combinedQuery.Add(drillDownQuery, Occur.MUST);
        }

        var result = luceneIndexService.UseSearcherWithFacets(
           index,
           query, 20,
           (searcher, facets) =>
           {
               var sortOptions = GetSortOption(sortBy);

               TopDocs topDocs;

               if (sortOptions != null)
               {
                   topDocs = searcher.Search(combinedQuery, MAX_RESULTS
                       , new Sort(sortOptions));
               }
               else
               {
                   topDocs = searcher.Search(combinedQuery, MAX_RESULTS);
               }

               return new GlobalSearchResultModel
               {
                   Query = searchText ?? "",
                   Page = page,
                   PageSize = pageSize,
                   TotalPages = topDocs.TotalHits <= 0 ? 0 : ((topDocs.TotalHits - 1) / pageSize) + 1,
                   TotalHits = topDocs.TotalHits,
                   Hits = topDocs.ScoreDocs
                       .Skip(offset)
                       .Take(limit)
                       .Select(d => MapToResultItem(searcher.Doc(d.Doc)))
                       .ToList(),
                   Facet = facet,
                   Facets = facets?.GetTopChildren(10, indexingStrategy.FacetDimension, new string[] { })?.LabelValues.ToArray(),
                   SortBy = sortBy
               };
           }
        );

        return result;
    }

    private static Query GetTermQuery(QueryBuilder queryBuilder, string searchText)
    {
        var booleanQuery = new BooleanQuery();

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            booleanQuery = AddToTermQuery(booleanQuery, queryBuilder.CreatePhraseQuery(nameof(GlobalSearchResultModel.Title), searchText, PHRASE_SLOP), 5);
            booleanQuery = AddToTermQuery(booleanQuery, queryBuilder.CreatePhraseQuery(ExampleSearchIndexingStrategy.CRAWLER_CONTENT_FIELD_NAME, searchText, PHRASE_SLOP), 1);
            booleanQuery = AddToTermQuery(booleanQuery, queryBuilder.CreateBooleanQuery(nameof(GlobalSearchResultModel.Title), searchText, Occur.SHOULD), 0.5f);
            booleanQuery = AddToTermQuery(booleanQuery, queryBuilder.CreateBooleanQuery(ExampleSearchIndexingStrategy.CRAWLER_CONTENT_FIELD_NAME, searchText, Occur.SHOULD), 0.1f);

            if (booleanQuery.GetClauses().Count() > 0)
            {
                return booleanQuery;
            }
        }

        return new MatchAllDocsQuery();
    }

    private static BooleanQuery AddToTermQuery(BooleanQuery query, Query textQueryPart, float boost)
    {
        if (textQueryPart != null)
        {
            textQueryPart.Boost = boost;
            query.Add(textQueryPart, Occur.SHOULD);
        }
        return query;
    }

    private SortField GetSortOption(string sortBy = null)
    {
        switch (sortBy)
        {
            case "a-z":
                return new SortField(ExampleSearchIndexingStrategy.SORTABLE_TITLE_FIELD_NAME, SortFieldType.STRING, false);
            case "z-a":
                return new SortField(ExampleSearchIndexingStrategy.SORTABLE_TITLE_FIELD_NAME, SortFieldType.STRING, true);
            default:
                return null;
        }
    }

    private GlobalSearchResultModel MapToResultItem(Document doc) => new()
    {
        Title = doc.Get(nameof(GlobalSearchResultModel.Title)),
        Url = doc.Get("Url"),
        ContentType = doc.Get(nameof(GlobalSearchResultModel.ContentType)),
    };
}

```

### Minimal changes

All you need to do to index more types is adding implementation for PageItems into this strategy. One strategy can implement all possible page types. Later on there is no need for a programmer to change the code as admin in the administration can chose which paths, languages, channels and content types are used in a specific page. There can be multiple search sites implemented using only one strategy - example usage is a typical website search. Search in a q and a site etc ...

### Display Results

Finally, display the strongly typed search results in a Razor View.

```xml
@foreach (var item in Model.Hits)
{
    <div class="row search-tile">
        <div class="col-md-8 col-lg-9 search-tile-content">
            <h3 class="h4 search-tile-title">
                <a href="@item.Url">@item.Title</a>
            </h3>
            <div class="search-tile-subtitle">@item.PublishedDate</div>
        </div>
    </div>
}
```

### Implementing document decay

You can score indexed items by "freshness" or "recency" using several techniques, each with different tradeoffs.

1. Boost relevant fields by setting field boost (preferable method, but requires more work).
2. Boost one field with constant value, that is always present in search query (shown in the example project, less desirable method.

   The Downside of this method is that all documents get matched, usable only for scenarios where total number of result is not required).

3. Use a sort expression. Implementation details can be found in Lucene.NET unit tests, Lucene.NET implementations

> Small differences in boosts will be ignored by Lucene.
