using Kentico.Xperience.Lucene.Core.Indexing;
using Kentico.Xperience.Lucene.Core.Search;

using Lucene.Net.Documents;
using Lucene.Net.Search;
using Lucene.Net.Util;

namespace DancingGoat.Search.Services;

public class SimpleSearchService
{
    private const int PHRASE_SLOP = 3;
    private const int MAX_RESULTS = 1000;

    private readonly ILuceneSearchService luceneSearchService;
    private readonly ILuceneIndexManager luceneIndexManager;

    public SimpleSearchService(
        ILuceneSearchService luceneSearchService,
        ILuceneIndexManager luceneIndexManager
    )
    {
        this.luceneSearchService = luceneSearchService;
        this.luceneIndexManager = luceneIndexManager;
    }

    public LuceneSearchResultModel<DancingGoatSearchResultModel> GlobalSearch(
        string indexName,
        string? searchText,
        int pageSize = 20,
        int page = 1)
    {
        var index = luceneIndexManager.GetRequiredIndex(indexName);
        var query = GetTermQuery(searchText, index);

        var result = luceneSearchService.UseSearcher(
           index,
           (searcher) =>
           {
               var topDocs = searcher.Search(query, MAX_RESULTS);

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

        var booleanQuery = new BooleanQuery();

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            booleanQuery = AddToTermQuery(booleanQuery, queryBuilder.CreatePhraseQuery(nameof(DancingGoatSearchResultModel.Title), searchText, PHRASE_SLOP), 5);
            booleanQuery = AddToTermQuery(booleanQuery, queryBuilder.CreateBooleanQuery(nameof(DancingGoatSearchResultModel.Title), searchText, Occur.SHOULD), 0.5f);

            if (booleanQuery.GetClauses().Count() > 0)
            {
                return booleanQuery;
            }
        }

        return new MatchAllDocsQuery();
    }

    private DancingGoatSearchResultModel MapToResultItem(Document doc) => new()
    {
        Title = doc.Get(nameof(DancingGoatSearchResultModel.Title)),
        Url = doc.Get("Url"),
        ContentType = doc.Get(nameof(DancingGoatSearchResultModel.ContentType)),
    };
}
