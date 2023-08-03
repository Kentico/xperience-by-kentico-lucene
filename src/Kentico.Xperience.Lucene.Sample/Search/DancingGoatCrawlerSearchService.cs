using Kentico.Xperience.Lucene;
using Kentico.Xperience.Lucene.Models;
using Kentico.Xperience.Lucene.Services;
using Lucene.Net.Documents;
using Lucene.Net.Search;
using Lucene.Net.Util;

namespace DancingGoat.Search;

public class DancingGoatCrawlerSearchService
{
    private const int PHRASE_SLOP = 3;
    private const int MAX_RESULTS = 1000;

    private readonly ILuceneIndexService luceneIndexService;

    public DancingGoatCrawlerSearchService(ILuceneIndexService luceneIndexService) => this.luceneIndexService = luceneIndexService;

    public LuceneSearchResultModel<DancingGoatCrawlerSearchResultModel> Search(string searchText, int pageSize = 20, int page = 1)
    {
        var index = IndexStore.Instance.GetIndex(DancingGoatCrawlerSearchModel.IndexName) ?? throw new Exception($"Index {DancingGoatSearchModel.IndexName} was not found!!!");
        pageSize = Math.Max(1, pageSize);
        page = Math.Max(1, page);
        int offset = pageSize * (page - 1);
        int limit = pageSize;

        var queryBuilder = new QueryBuilder(index.Analyzer);

        var query = string.IsNullOrWhiteSpace(searchText)
            ? new MatchAllDocsQuery()
            : GetTermQuery(queryBuilder, searchText);

        var result = luceneIndexService.UseSearcher(
            index,
            (searcher) =>
            {
                var topDocs = searcher.Search(query, MAX_RESULTS);

                return new LuceneSearchResultModel<DancingGoatCrawlerSearchResultModel>()
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
                };
            }
            );

        return result;
    }

    private static Query GetTermQuery(QueryBuilder queryBuilder, string searchText)
    {
        var titlePhrase = queryBuilder.CreatePhraseQuery(nameof(DancingGoatCrawlerSearchModel.Title), searchText, PHRASE_SLOP);
        titlePhrase.Boost = 5;
        var contentPhrase = queryBuilder.CreatePhraseQuery(nameof(DancingGoatCrawlerSearchModel.CrawlerContent), searchText, PHRASE_SLOP);
        contentPhrase.Boost = 1;
        var titleShould = queryBuilder.CreateBooleanQuery(nameof(DancingGoatCrawlerSearchModel.CrawlerContent), searchText, Occur.SHOULD);
        titleShould.Boost = 0.5f;
        var contentShould = queryBuilder.CreateBooleanQuery(nameof(DancingGoatCrawlerSearchModel.CrawlerContent), searchText, Occur.SHOULD);
        contentShould.Boost = 0.1f;

        return new BooleanQuery
        {
            { titlePhrase, Occur.SHOULD },
            { contentPhrase, Occur.SHOULD },
            { titleShould, Occur.SHOULD },
            { contentShould, Occur.SHOULD },
        };
    }

    private DancingGoatCrawlerSearchResultModel MapToResultItem(Document doc) => new()
    {
        Title = doc.Get(nameof(DancingGoatCrawlerSearchModel.Title)),
        Url = doc.Get(nameof(DancingGoatCrawlerSearchModel.Url)),
        ContentType = doc.Get(nameof(DancingGoatCrawlerSearchModel.ClassName)),
    };
}
