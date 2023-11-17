# Usage Guide

## Detailed Setup

### Create a Search Model

Define a custom (or multiple) `LuceneSearchModel` implementation to represent the content you want index.

```csharp
[IncludedPath("/%", ContentTypes = new string[] {
    AboutUs.CLASS_NAME,
    Home.CLASS_NAME,
})]
public class DancingGoatSearchModel : LuceneSearchModel
{
    public const string IndexName = "DancingGoat";

    [TextField(true)]
    [Source(new string[] { nameof(TreeNode.DocumentName) })]
    public string Title { get; set; }

    [TextField(false)]
    public string Content { get; set; }
}
```

### Create a custom Indexing Strategy (optional)

Define a custom `DefaultLuceneIndexingStrategy` implementation to customize how page content/fields are processed for the index.

```csharp
public class DancingGoatLuceneIndexingStrategy : DefaultLuceneIndexingStrategy
{
    public override async Task<object> OnIndexingProperty(TreeNode node, string propertyName, string usedColumn, object foundValue)
    {
        object result = foundValue;

        // Additional processing of the value

        return result;
    }
}
```

This indexing strategy allows you to hook into the indexing process by overriding the following methods of the `DefaultLuceneIndexingStrategy`:

- `OnIndexingProperty`
- `OnIndexingNode`
- `OnDocumentAddField`
- `FacetsConfigFactory`
- `ShouldIndexNode`

> For more details, review the `DancingGoatLuceneIndexingStrategy` in the example project.

### Register the Search Index

Add this library to the application services, registering your custom `LuceneSearchModel`.

```csharp
var builder = WebApplication.CreateBuilder(args);

// ...

builder.Services.AddLucene(new[]
{
    new LuceneIndex(
        typeof(MySearchModel),
        new StandardAnalyzer(Lucene.Net.Util.LuceneVersion.LUCENE_48),
        MySearchModel.IndexName,
        luceneIndexingStrategy: new MyCustomIndexingStrategy()),
});
```

You can add as many indexes as you want. Each index can have a different set of fields or store data for different [Content Types](https://docs.xperience.io/xp26/developers-and-admins/development/content-types).

### Rebuild the Search Index

The index will initially be empty until you create or modify some content.

To index all existing content, rebuild the index in Xperience's Administration within the Search application added by this library.

### Retrieve Content

Use the `ILuceneIndexService` (via DI) to retrieve the index populated by your custom `LuceneSearchModel`.

```csharp
public class DancingGoatSearchService
{
    private readonly ILuceneIndexService luceneIndexService;

    public DancingGoatSearchService(ILuceneIndexService luceneIndexService) =>
        this.luceneIndexService = luceneIndexService;

    public LuceneSearchResultModel<DancingGoatSearchModel> Search(
        string searchText, int pageSize = 20, int page = 1)
    {
        var index = IndexStore
            .Instance
            .GetIndex(DancingGoatSearchModel.IndexName);

        // ...
    }
}
```

Then, execute a search with a customized Lucene `Query` (like the `MatchAllDocsQuery`) using the ILuceneIndexService.

```csharp
// ...

var results = luceneIndexService.UseSearcher(index, (searcher) =>
{
    var topDocs = searcher.Search(query, MAX_RESULTS);

    return new LuceneSearchResultModel<DancingGoatSearchModel>()
    {
        Query = searchText,
        Page = page,
        PageSize = pageSize,
        TotalPages = topDocs.TotalHits <= 0 ? 0 : ((topDocs.TotalHits - 1) / pageSize) + 1,
        TotalHits = topDocs.TotalHits,
        Hits = topDocs.ScoreDocs
            .Skip(offset)
            .Take(limit)
            .Select(d => MapToResultItem(searcher.Doc(d.Doc)))
            .ToList(),
    };
}
```

> For a more advanced example, see the `CafeSearchService` in the example project.

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

Methods 1 and 2 require implementing `DefaultLuceneIndexingStrategy` and overriding `OnDocumentAddField` method.
In `OnDocumentAddField` match required fields and calculate boost, then apply to desired files as shown in example `DancingGoatLuceneIndexingStrategy.OnDocumentAddField`

> Small differences in boosts will be ignored by Lucene.

### Sample features

#### Trigger rebuild of index via webhook

Rebuild of index could be triggered by calling `POST` on webhook `/search/rebuild` with body

```json
{
  "indexName": "...",
  "secret": "..."
}
```

This could be used to trigger regular reindexing of content via CRON, Windows Task Scheduler or any other external scheduler.

## Additional Resources

- Review the "Search" functionality in the `examples/DancingGoat` Dancing Goat project to see how to implement search.
- Read the Lucene.NET [introduction](https://lucenenet.apache.org/) or [full documentation](https://lucenenet.apache.org/docs/4.8.0-beta00016/) to explore the core library's APIs and functionality.
- Explore the [Lucene.NET source on GitHub](https://github.com/apache/lucenenet)
