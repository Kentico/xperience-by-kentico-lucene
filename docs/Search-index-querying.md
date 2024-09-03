# Search index querying

## Rebuild the Search Index

Each index will initially be empty after creation until you create or modify some content.

To index all existing content, rebuild the index in Xperience's Administration within the Search application added by this library.

## Create a search result model

```csharp
public class GlobalSearchResultModel
{
    public string Title { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;

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
You can use your `GlobalSearchResultModel` as a generic parameter of prepared class template `LuceneSearchResultModel<T>` class to retrieve the most often desired data from `ILuceneSearchService`.

```csharp
public class SearchService
{
    private const int PHRASE_SLOP = 3;
    private const int MAX_RESULTS = 1000;

    private readonly ILuceneSearchService luceneSearchService;
    private readonly ILuceneIndexManager luceneIndexManager;
    private readonly ExampleSearchIndexingStrategy strategy;

    public SearchService(
        ILuceneSearchService luceneSearchService,
        ILuceneIndexManager luceneIndexManager,
        ExampleSearchIndexingStrategy strategy)
    {
        this.luceneSearchService = luceneSearchService;
        this.luceneIndexManager = luceneIndexManager;
        this.strategy = strategy;
    }

    public LuceneSearchResultModel<GlobalSearchResultModel> GlobalSearch(
        string indexName,
        string searchText,
        int pageSize = 20,
        int page = 1,
        string? facet = null,
        string? sortBy = null)
    {
        var index = luceneIndexManager.GetRequiredIndex(indexName);
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

                return new LuceneSearchResultModel<GlobalSearchResultModel>
                {
                    Query = searchText ?? string.Empty,
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
                    Facets = facets?.GetTopChildren(10, indexingStrategy.FacetDimension, new string[] { })?.LabelValues.ToArray()
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

Create a Controller which uses `SearchService` to display view with search bar.

```csharp
[Route("[controller]")]
[ApiController]
public class SearchController : Controller
{
    private readonly SearchService searchService;

    private const string NAME_OF_DEFAULT_INDEX = "Default";

    public SearchController(SearchService searchService)
    {
        this.searchService = searchService;
    }

    public IActionResult Index(string? query, int pageSize = 10, int page = 1, string? facet = null, string? sortBy = null, string? indexName = null)
    {
        try
        {
            var results = advancedSearchService.GlobalSearch(indexName ?? NAME_OF_DEFAULT_INDEX, query, pageSize, page, facet, sortBy);
            return View(results);
        }
        catch
        {
            return NotFound();
        }
    }
}
```

The controller retrieves `Index.cshtml` stored in `Views/Search/` solution folder. You can use `GetRouteData` method to assemble the parameters of the url of the endpoint defined in `SearchController`.

```cshtml
@model LuceneSearchResultModel<DancingGoatSearchResultModel>
@{
    Dictionary<string, string> GetRouteData(int page) =>
        new Dictionary<string, string>() { { "query", Model.Query }, { "pageSize", Model.PageSize.ToString() }, { "page", page.ToString() } };
}

<h1>Search</h1>

<style>
    .form-field {
        margin-bottom: 0.8rem;
    }
</style>

<div class="row" style="padding: 1rem;">
    <div class="col-12">
        <form asp-action="Index" method="get">
            <div class="row">
                <div class="col-md-12">
                    <div class="form-field">
                        <label class="control-label" asp-for="@Model.Query"></label>
                        <div class="editing-form-control-nested-control">
                            <input class="form-control" asp-for="@Model.Query" name="query">
                            <input type="hidden" asp-for="@Model.PageSize" name="pageSize" />
                            <input type="hidden" asp-for="@Model.Page" name="page" />
                        </div>
                    </div>
                </div>
            </div>

            <input type="submit" value="Submit">
        </form>
    </div>
</div>

@if (!Model.Hits.Any())
{
    if (!String.IsNullOrWhiteSpace(Model.Query))
    {
        @HtmlLocalizer["Sorry, no results match {0}", Model.Query]
    }

    return;
}

@foreach (var item in Model.Hits)
{
    <div class="row search-tile">
        <h3 class="h4 search-tile-title">
            <a href="@item.Url">@item.Title</a>
        </h3>
    </div>
}

<div class="pagination-container">
    <ul class="pagination">
        @if (Model.Page > 1)
        {
            <li class="PagedList-skipToPrevious">
                <a asp-controller="Search" asp-all-route-data="GetRouteData(Model.Page - 1)">
                    @HtmlLocalizer["previous"]
                </a>
            </li>
        }

        @for (int pageNumber = 1; pageNumber <= Model.TotalPages; pageNumber++)
        {
            if (pageNumber == Model.Page)
            {
                <li class="active">
                    <span>
                        @pageNumber
                    </span>
                </li>
            }
            else
            {
                <li>
                    <a asp-controller="Search" asp-all-route-data="GetRouteData(pageNumber)">@pageNumber</a>
                </li>
            }
        }

        @if (Model.Page < Model.TotalPages)
        {
            <li class="PagedList-skipToNext">
                <a asp-controller="Search" asp-all-route-data="GetRouteData(Model.Page + 1)">
                    @HtmlLocalizer["next"]
                </a>
            </li>
        }
    </ul>
</div>

```
