using Kentico.Xperience.Lucene.Models;
using Kentico.Xperience.Lucene.Services;
using Lucene.Net.Documents;
using Lucene.Net.Search;
using Lucene.Net.Util;
namespace Kentico.Xperience.Lucene.Examples.KBankNews
{
    public class KBankNewsSearchFacade
    {
        private const int PHRASE_SLOP = 3;
        private const int MAX_RESULTS = 1000;

        private readonly ILuceneIndexService luceneIndexService;

        public KBankNewsSearchFacade(ILuceneIndexService luceneIndexService) => this.luceneIndexService = luceneIndexService;
        public LuceneSearchResultModel<KBankNewsSearchResultItemModel> Search(string searchText, int pageSize = 20, int page = 1)
        {
            var index = IndexStore.Instance.GetIndex(KBankNewsSearchModel.IndexName) ?? throw new Exception($"Index {KBankNewsSearchModel.IndexName} was not found!!!");
            pageSize = Math.Max(1, pageSize);
            page = Math.Max(1, page);
            int offset = pageSize * (page - 1);
            int limit = pageSize;

            var queryBuilder = new QueryBuilder(index.Analyzer);

            var titlePhrase = queryBuilder.CreatePhraseQuery(nameof(KBankNewsSearchModel.Title), searchText, PHRASE_SLOP);
            titlePhrase.Boost = 5;
            var summaryPhrase = queryBuilder.CreatePhraseQuery(nameof(KBankNewsSearchModel.Summary), searchText, PHRASE_SLOP);
            summaryPhrase.Boost = 2;
            var contentPhrase = queryBuilder.CreatePhraseQuery(nameof(KBankNewsSearchModel.AllContent), searchText, PHRASE_SLOP);
            contentPhrase.Boost = 1;
            var titleShould = queryBuilder.CreateBooleanQuery(nameof(KBankNewsSearchModel.AllContent), searchText, Occur.SHOULD);
            titleShould.Boost = 0.5f;
            var contentShould = queryBuilder.CreateBooleanQuery(nameof(KBankNewsSearchModel.AllContent), searchText, Occur.SHOULD);
            contentShould.Boost = 0.1f;
            var query = new BooleanQuery
            {
                { titlePhrase, Occur.SHOULD },
                { summaryPhrase, Occur.SHOULD },
                { contentPhrase, Occur.SHOULD },
                { titleShould, Occur.SHOULD },
                { contentShould, Occur.SHOULD },
            };

            var result = luceneIndexService.UseSearcher(
                index,
                (searcher) =>
                {
                    var topDocs = searcher.Search(query, MAX_RESULTS);


                    return new LuceneSearchResultModel<KBankNewsSearchResultItemModel>()
                    {
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

        private KBankNewsSearchResultItemModel MapToResultItem(Document doc) => new()
        {
            Title = doc.Get(nameof(KBankNewsSearchModel.Title)),
            Summary = doc.Get(nameof(KBankNewsSearchModel.Summary)),
            NewsType = doc.Get(nameof(KBankNewsSearchModel.NewsType)),
            Url = doc.Get(nameof(KBankNewsSearchModel.Url)),
        };
    }
}
