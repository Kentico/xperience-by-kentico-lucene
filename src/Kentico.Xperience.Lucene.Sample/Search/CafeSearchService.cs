using Kentico.Xperience.Lucene;
using Kentico.Xperience.Lucene.Models;
using Kentico.Xperience.Lucene.Services;
using Lucene.Net.Documents;
using Lucene.Net.Facet;
using Lucene.Net.Search;
using Lucene.Net.Util;

namespace DancingGoat.Search;

public class CafeSearchService
{
    private const int PHRASE_SLOP = 3;
    private const int MAX_RESULTS = 1000;

    private readonly ILuceneIndexService luceneIndexService;

    public CafeSearchService(ILuceneIndexService luceneIndexService) => this.luceneIndexService = luceneIndexService;

    public LuceneSearchResultModel<CafeSearchModel> Search(string searchText, int pageSize = 20, int page = 1, string facet = null)
    {
        var index = IndexStore.Instance.GetIndex(CafeSearchModel.IndexName) ?? throw new Exception($"Index {CafeSearchModel.IndexName} was not found!!!");
        pageSize = Math.Max(1, pageSize);
        page = Math.Max(1, page);
        int offset = pageSize * (page - 1);
        int limit = pageSize;

        var queryBuilder = new QueryBuilder(index.Analyzer);

        var query = string.IsNullOrWhiteSpace(searchText)
            ? new MatchAllDocsQuery()
            : GetTermQuery(queryBuilder, searchText);

        DrillDownQuery drillDownQuery = null;
        if (facet != null)
        {
            var indexingStrategy = new CafeLuceneIndexingStrategy();
            var config = indexingStrategy.FacetsConfigFactory();
            drillDownQuery = new DrillDownQuery(indexingStrategy.FacetsConfigFactory(), query);

            string[] f = facet.Split(';', StringSplitOptions.RemoveEmptyEntries);
            if (f.Length >= 2)
            {
                var countryDim = config?.GetDimConfig("Country");
                var boolQuery = new BooleanQuery();
                boolQuery.Add(new TermQuery(DrillDownQuery.Term(countryDim.IndexFieldName, "Country", f.Skip(1).ToArray())), Occur.MUST);
                boolQuery.Add(query, Occur.MUST);
                drillDownQuery.Add("Country", boolQuery);
            }
        }

        var result = luceneIndexService.UseSearcherWithFacets(
            index,
            query, 20,
            (searcher, facets) =>
            {
                var topDocs = searcher.Search(drillDownQuery ?? query, MAX_RESULTS,
                    new Sort(new SortField(
                        nameof(DancingGoatSearchModel.PublishedDateTicks),
                        FieldCache.NUMERIC_UTILS_INT64_PARSER,
                        true)));

                return new LuceneSearchResultModel<CafeSearchModel>
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
                    Facets = facets?.GetTopChildren(10, "Country", facet?.Split(';').Skip(1).ToArray() ?? Array.Empty<string>())?.LabelValues
                };
            }
        );
        
        return result;
    }

    private static Query GetTermQuery(QueryBuilder queryBuilder, string searchText)
    {
        var titlePhrase = queryBuilder.CreatePhraseQuery(nameof(CafeSearchModel.Title), searchText, PHRASE_SLOP);
        titlePhrase.Boost = 5;

        return new BooleanQuery
        {
            { titlePhrase, Occur.SHOULD },
        };
    }

    private CafeSearchModel MapToResultItem(Document doc) => new()
    {
        Title = doc.Get(nameof(CafeSearchModel.Title)),
        Url = doc.Get(nameof(CafeSearchModel.Url)),
        CafeCity = doc.Get(nameof(CafeSearchModel.CafeCity)),
        CafeCountry = doc.Get(nameof(CafeSearchModel.CafeCountry)),
        CafeZipCode = doc.Get(nameof(CafeSearchModel.CafeZipCode)),
    };
}
