using DancingGoat.Search.Services;

namespace DancingGoat.Search;

public static class DancingGoatSearchStartupExtensions
{
    public static IServiceCollection AddLuceneServices(this IServiceCollection services)
    {
        services.AddLucene(builder => {
            builder.RegisterStrategy<AdvancedSearchIndexingStrategy>("DancingGoatExampleStrategy");
            builder.RegisterStrategy<SimpleSearchIndexingStrategy>("DancingGoatMinimalExampleStrategy");
        });

        services.AddHttpClient<WebCrawlerService>();
        services.AddSingleton<WebScraperHtmlSanitizer>();

        services.AddSingleton<SimpleSearchService>();
        services.AddSingleton<AdvancedSearchService>();

        return services;
    }
}
