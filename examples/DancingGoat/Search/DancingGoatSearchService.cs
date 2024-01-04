using System.Diagnostics;
using Kentico.Xperience.Lucene;
using Kentico.Xperience.Lucene.Models;
using Kentico.Xperience.Lucene.Services;
using Lucene.Net.Documents;
using Lucene.Net.Search;
using Lucene.Net.Util;

namespace DancingGoat.Search;

public class DancingGoatSearchService
{
    private const int PHRASE_SLOP = 3;
    private const int MAX_RESULTS = 1000;

    private readonly ILuceneIndexService luceneIndexService;

    public DancingGoatSearchService(ILuceneIndexService luceneIndexService) => this.luceneIndexService = luceneIndexService;

    public LuceneSearchResultModel<DancingGoatSearchModel> Search(string searchText, int pageSize = 20, int page = 1)
    {
        var index = IndexStore.Instance.GetIndex(DancingGoatSearchModel.IndexName) ?? throw new Exception($"Index {DancingGoatSearchModel.IndexName} was not found!!!");
        pageSize = Math.Max(1, pageSize);
        page = Math.Max(1, page);
        int offset = pageSize * (page - 1);
        int limit = pageSize;

        var queryBuilder = new QueryBuilder(index.Analyzer);

        var query = string.IsNullOrWhiteSpace(searchText)
            ? GetDefaultQuery(queryBuilder)
            : GetTermQuery(queryBuilder, searchText);

        var result = luceneIndexService.UseSearcher(
            index,
            (searcher) =>
            {
                var topDocs = searcher.Search(query, MAX_RESULTS
                    // uncomment if sort by score is not desirable 
                    // ,
                    // new Sort(new SortField(
                    //     nameof(DancingGoatSearchModel.PublishedDateTicks),
                    //     FieldCache.NUMERIC_UTILS_INT64_PARSER,
                    //     true))
                );
                foreach (var scoreDoc in topDocs.ScoreDocs
                             .Skip(offset)
                             .Take(limit))
                {
                    var explanation = searcher.Explain(query, scoreDoc.Doc);
                    Trace.WriteLine(explanation);
                }
                
                
                return new LuceneSearchResultModel<DancingGoatSearchModel>()
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

    private static Query GetDateRangeQuery()
    {
        long todayTicks = DateTools.TicksToUnixTimeMilliseconds(DateTime.Now.AddDays(1).Ticks);
        long lastYearTicks = DateTools.TicksToUnixTimeMilliseconds(DateTime.Now.AddYears(-2).Ticks);

        return NumericRangeQuery.NewInt64Range(
            nameof(DancingGoatSearchModel.PublishedDateTicks),
            NumericUtils.PRECISION_STEP_DEFAULT,
            lastYearTicks,
            todayTicks,
            true,
            true);
    }

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

    private static Query GetTermQuery(QueryBuilder queryBuilder, string searchText)
    {
        var titlePhrase = queryBuilder.CreatePhraseQuery(nameof(DancingGoatSearchModel.Title), searchText, PHRASE_SLOP);
        titlePhrase.Boost = 5;
        var summaryPhrase = queryBuilder.CreatePhraseQuery(nameof(DancingGoatSearchModel.ArticleSummary), searchText, PHRASE_SLOP);
        summaryPhrase.Boost = 2;
        var contentPhrase = queryBuilder.CreatePhraseQuery(nameof(DancingGoatSearchModel.AllContent), searchText, PHRASE_SLOP);
        contentPhrase.Boost = 1;
        var titleShould = queryBuilder.CreateBooleanQuery(nameof(DancingGoatSearchModel.AllContent), searchText, Occur.SHOULD);
        titleShould.Boost = 0.5f;
        var contentShould = queryBuilder.CreateBooleanQuery(nameof(DancingGoatSearchModel.AllContent), searchText, Occur.SHOULD);
        contentShould.Boost = 0.1f;
        
        // decay query, SHALL BE defined in queries where we require scoring by decay
        var decay = queryBuilder.CreateBooleanQuery("$decay", "q", Occur.SHOULD);
        decay.Boost = 0.01f;

        return new BooleanQuery
        {
            { decay, Occur.SHOULD },
            { titlePhrase, Occur.SHOULD },
            { summaryPhrase, Occur.SHOULD },
            { contentPhrase, Occur.SHOULD },
            { titleShould, Occur.SHOULD },
            { contentShould, Occur.SHOULD },
        };
    }

    private DancingGoatSearchModel MapToResultItem(Document doc) => new()
    {
        Title = doc.Get(nameof(DancingGoatSearchModel.Title)),
        ArticleSummary = doc.Get(nameof(DancingGoatSearchModel.ArticleSummary)),
        Url = doc.Get(nameof(DancingGoatSearchModel.Url)),
        ImagePath = doc.Get(nameof(DancingGoatSearchModel.ImagePath)),
        PublishedDateTicks = long.TryParse(doc.Get(nameof(DancingGoatSearchModel.PublishedDateTicks)), out long ticks)
            ? ticks
            : 0
    };
}
