using Kentico.Xperience.Lucene.Models;
using Lucene.Net.Analysis.Standard;

namespace DancingGoat.Search;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLuceneSearchServices(this IServiceCollection services)
    {
        var analyzer = new StandardAnalyzer(Lucene.Net.Util.LuceneVersion.LUCENE_48);

        services.AddLucene(new[]
        {
            new LuceneIndex(
                typeof(DancingGoatSearchModel),
                new StandardAnalyzer(Lucene.Net.Util.LuceneVersion.LUCENE_48),
                DancingGoatSearchModel.IndexName,
                "DancingGoatPages",
                "en",
                indexPath: null,
                new GlobalSearchModelIndexingStrategy()),

            new LuceneIndex(
                typeof(ArticleSearchModel),
                new StandardAnalyzer(Lucene.Net.Util.LuceneVersion.LUCENE_48),
                ArticleSearchModel.IndexName,
                "DancingGoatPages",
                "en",
                indexPath: null,
                new ArticleLuceneIndexingStrategy()),
        });

        services.AddSingleton<ArticleSearchService>();
        services.AddSingleton<WebScraperHtmlSanitizer>();
        services.AddHttpClient<WebCrawlerService>();
        services.AddSingleton<SearchService>();

        return services;
    }
}
