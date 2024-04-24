using DancingGoat.Search.Services;

using Lucene.Net.Analysis.Cz;

namespace DancingGoat.Search;

public static class DancingGoatSearchStartupExtensions
{
    public static IServiceCollection AddKenticoDancingGoatLuceneServices(this IServiceCollection services)
    {
        services.AddKenticoLucene(builder =>
        {
            builder.RegisterStrategy<AdvancedSearchIndexingStrategy>("DancingGoatExampleStrategy");
            builder.RegisterStrategy<SimpleSearchIndexingStrategy>("DancingGoatMinimalExampleStrategy");
            builder.RegisterAnalyzer<CzechAnalyzer>("Czech analyzer");
        });

        services.AddHttpClient<WebCrawlerService>();
        services.AddSingleton<WebScraperHtmlSanitizer>();

        services.AddSingleton<SimpleSearchService>();
        services.AddSingleton<AdvancedSearchService>();

        return services;
    }
}
