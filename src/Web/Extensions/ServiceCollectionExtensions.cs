using DancingGoat.Models;
using Kentico.Xperience.Lucene.Models;
using Lucene.Net.Analysis.Standard;
using Microsoft.Extensions.DependencyInjection;

namespace DancingGoat.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLuceneSearchServices(this IServiceCollection services)
    {
        var analyzer = new StandardAnalyzer(Lucene.Net.Util.LuceneVersion.LUCENE_48);

        services.AddLucene(new[]
        {
            new LuceneIndex(
                typeof(GlobalSearchModel),
                new StandardAnalyzer(Lucene.Net.Util.LuceneVersion.LUCENE_48),
                GlobalSearchModel.IndexName,
                "DancingGoatPages",
                "en",
                indexPath: null,
                new GlobalSearchModelIndexingStrategy()),
        });

        services.AddSingleton<WebScraperHtmlSanitizer>();
        services.AddHttpClient<WebCrawlerService>();
        services.AddSingleton<SearchService>();

        return services;
    }
}
