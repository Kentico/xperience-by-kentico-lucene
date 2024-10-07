using Kentico.Xperience.Lucene.Core.Indexing;
using Kentico.Xperience.Lucene.Core.Search;

using Lucene.Net.Documents;
using Lucene.Net.Facet;
using Lucene.Net.Search;
using Lucene.Net.Util;

namespace DancingGoat.Search.Services;

public class AdvancedSearchService
{
    private const int PHRASE_SLOP = 3;
    private const int MAX_RESULTS = 1000;

    private readonly ILuceneSearchService luceneSearchService;
    private readonly ILuceneIndexManager luceneIndexManager;
    private readonly AdvancedSearchIndexingStrategy strategy;

    public AdvancedSearchService(
        ILuceneSearchService luceneSearchService,
        ILuceneIndexManager luceneIndexManager,
        AdvancedSearchIndexingStrategy strategy
    )
    {
        this.luceneSearchService = luceneSearchService;
        this.strategy = strategy;
        this.luceneIndexManager = luceneIndexManager;
    }

    public LuceneSearchResultModel<DancingGoatSearchResultModel> GlobalSearch(
        string indexName,
        string? searchText,
        int pageSize = 20,
        int page = 1,
        string? facet = null,
        string? sortBy = null)
    {
        var index = luceneIndexManager.GetRequiredIndex(indexName);
        var query = GetTermQuery(searchText, index);

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
                drillDownQuery.Add(nameof(AdvancedSearchIndexingStrategy.FACET_DIMENSION), subFacet);
            }

            combinedQuery.Add(drillDownQuery, Occur.MUST);
        }

        var result = luceneSearchService.UseSearcherWithFacets(
           index,
           query, 20,
           (searcher, facets) =>
           {
               var sortOptions = GetSortOption(sortBy);

               var topDocs = sortOptions != null
                   ? searcher.Search(combinedQuery, MAX_RESULTS, new Sort(sortOptions))
                   : searcher.Search(combinedQuery, MAX_RESULTS);

               pageSize = Math.Max(1, pageSize);
               page = Math.Max(1, page);

               int offset = pageSize * (page - 1);
               int limit = pageSize;

               return new LuceneSearchResultModel<DancingGoatSearchResultModel>
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
                   Facets = facets?.GetTopChildren(10, AdvancedSearchIndexingStrategy.FACET_DIMENSION, new string[] { })?.LabelValues.ToArray(),
               };
           }
        );

        return result;
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

    private Query GetTermQuery(string? searchText, LuceneIndex index)
    {
        var analyzer = index.LuceneAnalyzer;
        var queryBuilder = new QueryBuilder(analyzer);

        if (string.IsNullOrEmpty(searchText))
        {
            return GetDefaultQuery(queryBuilder);
        }
        var booleanQuery = new BooleanQuery();

        booleanQuery = AddToTermQuery(booleanQuery, queryBuilder.CreatePhraseQuery(nameof(DancingGoatSearchResultModel.Title), searchText, PHRASE_SLOP), 5);
        booleanQuery = AddToTermQuery(booleanQuery, queryBuilder.CreatePhraseQuery(AdvancedSearchIndexingStrategy.CRAWLER_CONTENT_FIELD_NAME, searchText, PHRASE_SLOP), 1);
        booleanQuery = AddToTermQuery(booleanQuery, queryBuilder.CreateBooleanQuery(nameof(DancingGoatSearchResultModel.Title), searchText, Occur.SHOULD), 0.5f);
        booleanQuery = AddToTermQuery(booleanQuery, queryBuilder.CreateBooleanQuery(AdvancedSearchIndexingStrategy.CRAWLER_CONTENT_FIELD_NAME, searchText, Occur.SHOULD), 0.1f);

        if (booleanQuery.GetClauses().Count() > 0)
        {
            return booleanQuery;
        }

        return new MatchAllDocsQuery();
    }

    private SortField? GetSortOption(string? sortBy = null)
    {
        switch (sortBy)
        {
            case "a-z":
                return new SortField(AdvancedSearchIndexingStrategy.SORTABLE_TITLE_FIELD_NAME, SortFieldType.STRING, false);
            case "z-a":
                return new SortField(AdvancedSearchIndexingStrategy.SORTABLE_TITLE_FIELD_NAME, SortFieldType.STRING, true);
            default:
                return null;
        }
    }

    private DancingGoatSearchResultModel MapToResultItem(Document doc) => new()
    {
        Title = doc.Get(nameof(DancingGoatSearchResultModel.Title)),
        Url = doc.Get("Url"),
        ContentType = doc.Get(nameof(DancingGoatSearchResultModel.ContentType)),
    };

    private static Query GetDefaultQuery(QueryBuilder queryBuilder)
    {
        // decay query, SHALL BE defined in queries where we require scoring by decay
        var decay = queryBuilder.CreateBooleanQuery("$decay", "q", Occur.SHOULD);
        decay.Boost = 0.01f;

        return new BooleanQuery
        {
            { decay, Occur.SHOULD }
        };
    }
}
