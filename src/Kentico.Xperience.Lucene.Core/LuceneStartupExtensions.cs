using Kentico.Xperience.Lucene.Core;
using Kentico.Xperience.Lucene.Core.Indexing;
using Kentico.Xperience.Lucene.Core.Search;

using Lucene.Net.Analysis.Standard;

using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class LuceneStartupExtensions
{
    /// <summary>
    /// Adds Lucene services and custom module to application using the <see cref="DefaultLuceneIndexingStrategy"/> and <see cref="StandardAnalyzer"/> for all indexes.
    /// </summary>
    /// <param name="serviceCollection">the <see cref="IServiceCollection"/> which will be modified.</param>
    /// <param name="configuration">the <see cref="IConfiguration"/> which will be used to configure the <see cref="LuceneSearchOptions"/>.</param>
    /// <returns>Returns this instance of <see cref="IServiceCollection"/>, allowing for further configuration in a fluent manner.</returns>
    public static IServiceCollection AddKenticoLucene(this IServiceCollection serviceCollection, IConfiguration? configuration = null)
    {
        serviceCollection.AddLuceneServicesInternal(configuration);

        StrategyStorage.AddStrategy<DefaultLuceneIndexingStrategy>("Default");
        AnalyzerStorage.AddAnalyzer<StandardAnalyzer>("Standard");

        return serviceCollection;
    }


    /// <summary>
    /// Adds Lucene services and custom module to application with customized options provided by the <see cref="ILuceneBuilder"/>
    /// in the <paramref name="configure" /> action.
    /// </summary>
    /// <param name="serviceCollection">the <see cref="IServiceCollection"/> which will be modified.</param>
    /// <param name="configure"><see cref="Action"/> which will configure the <see cref="ILuceneBuilder"/>.</param>
    /// <param name="configuration">the <see cref="IConfiguration"/> which will be used to configure the <see cref="LuceneSearchOptions"/>.</param>
    /// <returns>Returns this instance of <see cref="IServiceCollection"/>, allowing for further configuration in a fluent manner.</returns>
    public static IServiceCollection AddKenticoLucene(this IServiceCollection serviceCollection, Action<ILuceneBuilder> configure, IConfiguration? configuration = null)
    {
        serviceCollection.AddLuceneServicesInternal(configuration);

        var builder = new LuceneBuilder(serviceCollection);

        configure(builder);

        if (builder.IncludeDefaultStrategy)
        {
            builder.RegisterStrategy<DefaultLuceneIndexingStrategy>("Default");
        }

        if (builder.IncludeDefaultAnalyzer)
        {
            builder.RegisterAnalyzer<StandardAnalyzer>("Standard");
        }

        return serviceCollection;
    }


    private static IServiceCollection AddLuceneServicesInternal(this IServiceCollection services, IConfiguration? configuration = null)
    {
        if (configuration is not null)
        {
            services.Configure<LuceneSearchOptions>(
                configuration.GetSection(LuceneSearchOptions.CMS_LUCENE_SEARCH_SECTION_NAME)
            );
        }

        services
            .AddSingleton<LuceneModuleInstaller>()
            .AddSingleton<ILuceneClient, DefaultLuceneClient>()
            .AddSingleton<ILuceneTaskLogger, DefaultLuceneTaskLogger>()
            .AddSingleton<ILuceneTaskProcessor, DefaultLuceneTaskProcessor>()
            .AddSingleton<ILuceneConfigurationStorageService, DefaultLuceneConfigurationStorageService>()
            .AddSingleton<ILuceneIndexService, DefaultLuceneIndexService>()
            .AddSingleton<ILuceneSearchService, DefaultLuceneSearchService>()
            .AddSingleton<ILuceneIndexManager, DefaultLuceneIndexManager>()
            .AddTransient<DefaultLuceneIndexingStrategy>()
            .AddHostedService<LuceneAutomaticReindexingBackgroundService>();

        return services;
    }
}
