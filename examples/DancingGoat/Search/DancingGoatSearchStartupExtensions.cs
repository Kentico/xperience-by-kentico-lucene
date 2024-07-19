using DancingGoat.Search.Services;

using Path = CMS.IO.Path;

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
            builder.SetLuceneStoragePathBase(Path.Combine(Environment.CurrentDirectory, "App_Data", "Lucene"));
        });

        services.AddHttpClient<WebCrawlerService>();
        services.AddSingleton<WebScraperHtmlSanitizer>();

        services.AddSingleton<SimpleSearchService>();
        services.AddSingleton<AdvancedSearchService>();

        return services;
    }
}
