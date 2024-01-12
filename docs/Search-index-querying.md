# Search index querying

> Note: this part of the documentation is still a work-in-progress and will be updated with an example Dancing Goat project

## Rebuild the Search Index

Each index will initially be empty after creation until you create or modify some content.

To index all existing content, rebuild the index in Xperience's Administration within the Search application added by this library.

## Create a search result model

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

## Create a search service

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

## Display Results

... TODO
