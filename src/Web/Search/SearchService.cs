using Kentico.Xperience.Lucene;
using Kentico.Xperience.Lucene.Models;
using Kentico.Xperience.Lucene.Services;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Search;
using Lucene.Net.Util;

namespace DancingGoat.Search;

public class SearchService
{
    private const int PHRASE_SLOP = 3;
    private const int MAX_RESULTS = 1000;

    private readonly ILuceneIndexService luceneIndexService;

    public SearchService(ILuceneIndexService luceneIndexService) => this.luceneIndexService = luceneIndexService;

    public LuceneSearchResultModel<GlobalSearchResultModel> GlobalSearch(string searchText, int pageSize = 20, int page = 1)
    {
        var index = IndexStore.Instance.GetIndex(DancingGoatSearchModel.IndexName) ?? throw new Exception($"Index {DancingGoatSearchModel.IndexName} was not found!!!");
        pageSize = Math.Max(1, pageSize);
        page = Math.Max(1, page);

        int offset = pageSize * (page - 1);
        int limit = pageSize;

        var analyzer = new StandardAnalyzer(LuceneVersion.LUCENE_48);

        var queryBuilder = new QueryBuilder(analyzer);

        var query = string.IsNullOrWhiteSpace(searchText)
            ? new MatchAllDocsQuery()
            : GetTermQuery(queryBuilder, searchText);

        var indexingStrategy = new GlobalSearchModelIndexingStrategy();

        var combinedQuery = new BooleanQuery();

        combinedQuery.Add(query, Occur.MUST);

        var result = luceneIndexService.UseSearcher(
            index,
            (searcher) =>
            {
                var topDocs = searcher.Search(query, MAX_RESULTS);

                return new LuceneSearchResultModel<GlobalSearchResultModel>()
                {
                    Query = searchText ?? "",
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = topDocs.TotalHits <= 0 ? 0 : (topDocs.TotalHits - 1) / pageSize + 1,
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

    private static Query GetTermQuery(QueryBuilder queryBuilder, string searchText)
    {
        var booleanQuery = new BooleanQuery();

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            booleanQuery = AddToTermQuery(booleanQuery, queryBuilder.CreatePhraseQuery(nameof(DancingGoatSearchModel.Title), searchText, PHRASE_SLOP), 5);
            booleanQuery = AddToTermQuery(booleanQuery, queryBuilder.CreatePhraseQuery(nameof(DancingGoatSearchModel.CrawlerContent), searchText, PHRASE_SLOP), 1);
            booleanQuery = AddToTermQuery(booleanQuery, queryBuilder.CreateBooleanQuery(nameof(DancingGoatSearchModel.Title), searchText, Occur.SHOULD), 0.5f);
            booleanQuery = AddToTermQuery(booleanQuery, queryBuilder.CreateBooleanQuery(nameof(DancingGoatSearchModel.CrawlerContent), searchText, Occur.SHOULD), 0.1f);

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

    private GlobalSearchResultModel MapToResultItem(Document doc) => new()
    {
        Title = doc.Get(nameof(DancingGoatSearchModel.Title)),
        Url = doc.Get(nameof(DancingGoatSearchModel.Url)),
        ContentType = doc.Get(nameof(DancingGoatSearchModel.ClassName)),
    };
}
