# Usage Guide

This library supports using Lucene.NET to index both unstructured and high structured, interrelated content in an Xperience by Kentico solution. This indexed content can then be programmatically queried and displayed in a website channel.

Below are the steps to integrate the library into your solution.

## Create a custom Indexing Strategy

Define a custom `DefaultLuceneIndexingStrategy` implementation to customize how page or content items are processed for indexing.

Your custom implemention of `DefaultLuceneIndexingStrategy` can use dependency injection to define services and configuration used for gathering the content to be indexed. `DefaultLuceneIndexingStrategy` implements `ILuceneIndexingStrategy` and will be [registered as a transient](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection#transient) in the DI container.

## Specify a mapping process

Override the `Task<Document?> MapToLuceneDocumentOrNull(IIndexEventItemModel item)` method and define a process for mapping custom fields of each content item event provided.

The method is given an `IIndexEventItemModel` which is a abstraction of any item being processed for indexing, which includes both `IndexEventWebPageItemModel` for web page items and `IndexEventReusableItemModel` for reusable content items. Every item specified in the admin ui is rebuilt. In the UI you need to specify one or more language, channel name, indexingStrategy and paths with content types. This strategy than evaluates all web page items specified in the administration.

Let's say we specified `ArticlePage` in the admin ui.
Now we implement how we want to save ArticlePage document in our strategy.

The document is indexed representation of the webpageitem.

You specify what fields should be indexed in the document by adding this to the document. You later retrieve data from document based on your implementation.

```csharp
public class ExampleSearchIndexingStrategy : DefaultLuceneIndexingStrategy
{
    public static string SORTABLE_TITLE_FIELD_NAME = "SortableTitle";

    public override async Task<Document?> MapToLuceneDocumentOrNull(IIndexEventItemModel item)
    {
        var document = new Document();

        string sortableTitle = "";
        string title = "";

        // IIndexEventItemModel could be a reusable content item or a web page item, so we use
        // pattern matching to get access to the web page item specific type and fields
        if (item is IndexEventWebPageItemModel webpageItem &&
            string.Equals(indexedModel.ContentTypeName, ArticlePage.CONTENT_TYPE_NAME, StringComparison.OrdinalIgnorecase))
        {
            // The implementation of GetPage<T>() is detailed below
            var page = await GetPage<ArticlePage>(
                webpageItem.ItemGuid,
                webpageItem.WebsiteChannelName,
                webpageItem.LanguageName,
                ArticlePage.CONTENT_TYPE_NAME);

            if (page is null)
            {
                return null;
            }

            var article = page.ArticlePageArticle.FirstOrDefault();

            if (article == null)
            {
                return null;
            }

            var article = page.ArticlePageArticle.FirstOrDefault();

            sortableTitle = title = article?.ArticleTitle ?? "";
        }

        document.Add(new TextField(nameof(GlobalSearchResultModel.Title), title, Field.Store.YES));
        document.Add(new StringField(SORTABLE_TITLE_FIELD_NAME, sortableTitle, Field.Store.YES));

        return document;
    }
}
```

Some properties of the `IIndexEventItemModel` are added to the document by default by the library and these can be found in the `BaseDocumentProperties` class.
These can be retrieved from any indexed document. by the value of the constants in that class.

```csharp
// BaseDocumentProperties.cs

public static class BaseDocumentProperties
{
    // View source code for full list
    public const string ID = "ID";
    public const string CONTENT_TYPE_NAME = "ContentTypeName";
    // ...
    public const string URL = "Url";
}
```

The `Url` field is a relative path by default. You can change this by adding this field manually in the `MapToLuceneDocumentOrNull` method.

```csharp
public override async Task<Document?> MapToLuceneDocumentOrNull(IIndexEventItemModel item)
{
    //...

    var document = new Document();

    // retrieve an absolute URL
    if (item is IndexEventWebPageItemModel webpageItem &&
        string.Equals(indexedModel.ContentTypeName, ArticlePage.CONTENT_TYPE_NAME, StringComparison.OrdinalIgnorecase))
    {
        string url = string.Empty;
        try
        {
            url = (await urlRetriever.Retrieve(
                webpageItem.WebPageItemTreePath,
                webpageItem.WebsiteChannelName,
                webpageItem.LanguageName)).AbsolutePath;
        }
        catch (Exception)
        {
            // Retrieve can throw an exception when processing a page update LuceneQueueItem
            // and the page was deleted before the update task has processed. In this case, upsert an
            // empty URL
        }

        document.AddStringField(BaseDocumentProperties.URL, url, Field.Store.YES);
    }

    //...
}
```

## Add facets

We can also specify a facet dimension. Which is used in your index querying code if you want to create a faceted search experience.

```csharp
public class ExampleSearchIndexingStrategy : DefaultLuceneIndexingStrategy
{
    public string FacetDimension { get; set; } = "ContentType";

    public override async Task<Document?> MapToLuceneDocumentOrNull(IIndexEventItemModel item)
    {
        var document = new Document();

        string sortableTitle = "";
        string title = "";
        string contentType = "";

        if (item is IndexEventWebPageItemModel webpageItem &&
            string.Equals(indexedModel.ContentTypeName, ArticlePage.CONTENT_TYPE_NAME, StringComparison.OrdinalIgnorecase))
        {
            // Same as the first example
            // ...

            contentType = "news";
        }

        // Add the facet value as a facet field
        document.Add(new FacetField(FacetDimension, contentType));

        // Set other fields
        // ...

        return document;
    }

    public override FacetsConfig FacetsConfigFactory()
    {
        var facetConfig = new FacetsConfig();

        facetConfig.SetMultiValued(FacetDimension, true);

        return facetConfig;
    }
}
```

## Data retrieval during indexing

It is up to your implementation how do you want to retrieve the content or data to be indexed, however any web page item could be retrieved using a generic `GetPage<T>` method. In the example below, you specify that you want to retrieve `ArticlePage` item in the provided language on the channel using provided id and content type.

```csharp
public class ExampleSearchIndexingStrategy : DefaultLuceneIndexingStrategy
{
    // Other fields defined in previous examples
    // ...

    private readonly IWebPageQueryResultMapper webPageMapper;
    private readonly IContentQueryExecutor queryExecutor;

    public ExampleSearchIndexingStrategy(
        IWebPageQueryResultMapper webPageMapper,
        IContentQueryExecutor queryExecutor,
    )
    {
        this.webPageMapper = webPageMapper;
        this.queryExecutor = queryExecutor;
    }

    public override async Task<Document?> MapToLuceneDocumentOrNull(IIndexEventItemModel item)
    {
        // Implementation detailed in previous examples, including GetPage<T> call
        // ...
    }

    public override FacetsConfig FacetsConfigFactory()
    {
        // Same as examples above
        // ...
    }

    private async Task<T?> GetPage<T>(Guid id, string channelName, string languageName, string contentTypeName)
        where T : IWebPageFieldsSource, new()
    {
        var query = new ContentItemQueryBuilder()
            .ForContentType(contentTypeName,
                config =>
                    config
                        .WithLinkedItems(4) // You could parameterize this if you want to optimize specific database queries
                        .ForWebsite(channelName)
                        .Where(where => where.WhereEquals(nameof(WebPageFields.WebPageItemGUID), id))
                        .TopN(1))
            .InLanguage(languageName);

        var result = await queryExecutor.GetWebPageResult(query, webPageMapper.Map<T>);

        return result.FirstOrDefault();
    }
}
```

## Keeping indexed related content up to date

If an indexed web page item has relationships to other web page items or reusable content items, and updates to those items should trigger
a reindex of the original web page item, you can override the `Task<IEnumerable<IIndexEventItemModel>> FindItemsToReindex(IndexEventWebPageItemModel changedItem)` or `Task<IEnumerable<IIndexEventItemModel>> FindItemsToReindex(IndexEventReusableItemModel changedItem)` methods which both return the items that should be indexed based on the incoming item being changed.

In our example an `ArticlePage` web page item has a `ArticlePageArticle` field which represents a reference to related reusable content items that contain the full article content. We include content from the reusable item in our indexed web page, so changes to the reusable item should result in the index being updated for the web page item.

All items returned from either `FindItemsToReindex` method will be passed to `Task<Document?> MapToLuceneDocumentOrNull(IIndexEventItemModel item)` for indexing.

```csharp
public class ExampleSearchIndexingStrategy : DefaultLuceneIndexingStrategy
{
    // Other fields defined in previous examples
    // ...

    public const string INDEXED_WEBSITECHANNEL_NAME = "mywebsitechannel";

    private readonly IWebPageQueryResultMapper webPageMapper;
    private readonly IContentQueryExecutor queryExecutor;

    public ExampleSearchIndexingStrategy(
        IWebPageQueryResultMapper webPageMapper,
        IContentQueryExecutor queryExecutor,
    )
    {
        this.webPageMapper = webPageMapper;
        this.queryExecutor = queryExecutor;
    }

    public override async Task<Document?> MapToLuceneDocumentOrNull(IIndexEventItemModel item)
    {
        // Implementation detailed in previous examples, including GetPage<T> call
        // ...
    }

    public override async Task<IEnumerable<IIndexEventItemModel>> FindItemsToReindex(IndexEventReusableItemModel changedItem)
    {
        var reindexedItems = new List<IIndexEventItemModel>();

        if (string.Equals(indexedModel.ContentTypeName, Article.CONTENT_TYPE_NAME, StringComparison.OrdinalIgnorecase))
        {
            var query = new ContentItemQueryBuilder()
                .ForContentType(ArticlePage.CONTENT_TYPE_NAME,
                    config =>
                        config
                            .WithLinkedItems(4)

                            // Because the changedItem is a reusable content item, we don't have a website channel name to use here
                            // so we use a hardcoded channel name.
                            //
                            // This will be resolved with an upcoming Xperience by Kentico feature
                            // https://roadmap.kentico.com/c/193-new-api-cross-content-type-querying
                            .ForWebsite(INDEXED_WEBSITECHANNEL_NAME)

                            // Retrieves all ArticlePages that link to the Article through the ArticlePage.ArticlePageArticle field
                            .Linking(nameof(ArticlePage.ArticlePageArticle), new[] { changedItem.ItemID }))
                .InLanguage(changedItem.LanguageName);

            var result = await queryExecutor.GetWebPageResult(query, webPageMapper.Map<ArticlePage>);

            foreach (var articlePage in result)
            {
                // This will be a IIndexEventItemModel passed to our MapToLuceneDocumentOrNull method above
                reindexable.Add(new IndexEventWebPageItemModel(
                    page.SystemFields.WebPageItemID,
                    page.SystemFields.WebPageItemGUID,
                    changedItem.LanguageName,
                    ArticlePage.CONTENT_TYPE_NAME,
                    page.SystemFields.WebPageItemName,
                    page.SystemFields.ContentItemIsSecured,
                    page.SystemFields.ContentItemContentTypeID,
                    page.SystemFields.ContentItemCommonDataContentLanguageID,
                    INDEXED_WEBSITECHANNEL_NAME,
                    page.SystemFields.WebPageItemTreePath,
                    page.SystemFields.WebPageItemParentID,
                    page.SystemFields.WebPageItemOrder));
            }
        }

        return reindexedItems;
    }

    public override FacetsConfig FacetsConfigFactory()
    {
        // Same as examples above
        // ...
    }

    private async Task<T?> GetPage<T>(Guid id, string channelName, string languageName, string contentTypeName)
        where T : IWebPageFieldsSource, new()
    {
        // Same as examples above
        // ...
    }
}
```

Note that we are not preparing the Lucene `Document` in `FindItemsToReindex`, but instead are generating a collection of
additional items that will need reindexing based on the modification of a related `IIndexEventItemModel`.

## Register the custom strategy and other dependencies in DI

Add this library to the application services, registering your custom `DefaultLuceneIndexingStrategy` and Lucene

```csharp
// Program.cs

// Registers all services and uses default indexing behavior (no custom data will be indexed)
services.AddLucene();

// or

// Registers all services and enables custom indexing behavior
services.AddLucene(builder =>
    builder
        .RegisterStrategy<ExampleSearchIndexingStrategy>("ExampleStrategy"));
```

## Indexing web page content

See [Scraping web page content](Scraping-web-page-content.md)

## Search index querying

> Note: this part of the documentation is still a work-in-progress and will be updated with an example Dancing Goat project

### Rebuild the Search Index

Each index will initially be empty after creation until you create or modify some content.

To index all existing content, rebuild the index in Xperience's Administration within the Search application added by this library.

### Create a search result model

```csharp
public class GlobalSearchResultModel
{
    public string Title { get; set; } = "";
    public string ContentType { get; set; } = "";
    public string Url { get; set; } = "";

    public static List<string> PossibleFacets { get; set; } = new List<string>
    {
        "other",
        "news",
        "product",
    };
}
```

### Create a search service

Execute a search with a customized Lucene `Query` (like the `MatchAllDocsQuery`) using the ILuceneSearchService.

```csharp
public class SearchService
{
    private const int PHRASE_SLOP = 3;
    private const int MAX_RESULTS = 1000;

    private readonly ILuceneSearchService luceneSearchService;
    private readonly ExampleSearchIndexingStrategy strategy;

    public SearchService(
        ILuceneSearchService luceneSearchService,
        ExampleSearchIndexingStrategy strategy)
    {
        this.luceneSearchService = luceneSearchService;
        this.strategy = strategy;
    }

    public GlobalSearchResultModel GlobalSearch(
        string indexName,
        string searchText,
        int pageSize = 20,
        int page = 1,
        string? facet = null,
        string? sortBy = null)
    {
        var index = LuceneIndexStore.Instance.GetRequiredIndex(indexName);
        var query = GetTermQuery(searchText, facet, sortBy);

        var combinedQuery = new BooleanQuery
        {
            { query, Occur.MUST }
        };

        if (facet != null)
        {
            var drillDownQuery = new DrillDownQuery(strategy.FacetsConfigFactory());

            string[] subFacets = facet.Split(';', StringSplitOptions.RemoveEmptyEntries);

            foreach (string subFacet in subFacets)
            {
                drillDownQuery.Add(nameof(BlogSearchModel.Taxonomy), subFacet);
            }

            combinedQuery.Add(drillDownQuery, Occur.MUST);
        }

        var result = luceneSearchService.UseSearcherWithFacets(
           index,
           query, 20,
           (searcher, facets) =>
           {
                var sortOptions = GetSortOption(sortBy);

                TopDocs topDocs;

                if (sortOptions != null)
                {
                    topDocs = searcher.Search(combinedQuery, MAX_RESULTS, new Sort(sortOptions));
                }
                else
                {
                    topDocs = searcher.Search(combinedQuery, MAX_RESULTS);
                }

                pageSize = Math.Max(1, pageSize);
                page = Math.Max(1, page);

                int offset = pageSize * (page - 1);
                int limit = pageSize;

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

    private static Query GetTermQuery(string searchText, string? facet, string? sortBy)
    {
        // TODO - query building example
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

### Display Results

... TODO

## Implementing document decay

You can score indexed items by "freshness" or "recency" using several techniques, each with different tradeoffs.

1. Boost relevant fields by setting field boost (preferable method, but requires more work).
2. Boost one field with constant value, that is always present in search query (shown in the example project, less desirable method.

   The Downside of this method is that all documents get matched, usable only for scenarios where total number of result is not required).

3. Use a sort expression. Implementation details can be found in Lucene.NET unit tests, Lucene.NET implementations

> Small differences in boosts will be ignored by Lucene.
