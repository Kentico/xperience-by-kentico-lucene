using Kentico.Xperience.Lucene.Core;
using Kentico.Xperience.Lucene.Core.Indexing;
using Kentico.Xperience.Lucene.Core.Search;

using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Util;

namespace Microsoft.Extensions.DependencyInjection;

public static class LuceneStartupExtensions
{
    /// <summary>
    /// Adds Lucene services and custom module to application using the <see cref="DefaultLuceneIndexingStrategy"/> and <see cref="StandardAnalyzer"/> for all indexes
    /// </summary>
    /// <param name="serviceCollection">the <see cref="IServiceCollection"/> which will be modified</param>
    /// <returns>Returns this instance of <see cref="IServiceCollection"/>, allowing for further configuration in a fluent manner.</returns>
    public static IServiceCollection AddKenticoLucene(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddLuceneServicesInternal();

        StrategyStorage.AddStrategy<DefaultLuceneIndexingStrategy>("Default");
        AnalyzerStorage.AddAnalyzer<StandardAnalyzer>("Standard");

        return serviceCollection;
    }


    /// <summary>
    /// Adds Lucene services and custom module to application with customized options provided by the <see cref="ILuceneBuilder"/>
    /// in the <paramref name="configure" /> action.
    /// </summary>
    /// <param name="serviceCollection">the <see cref="IServiceCollection"/> which will be modified</param>
    /// <param name="configure"><see cref="Action"/> which will configure the <see cref="ILuceneBuilder"/></param>
    /// <returns>Returns this instance of <see cref="IServiceCollection"/>, allowing for further configuration in a fluent manner.</returns>
    public static IServiceCollection AddKenticoLucene(this IServiceCollection serviceCollection, Action<ILuceneBuilder> configure)
    {
        serviceCollection.AddLuceneServicesInternal();

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


    private static IServiceCollection AddLuceneServicesInternal(this IServiceCollection services) =>
        services
            .AddSingleton<LuceneModuleInstaller>()
            .AddSingleton<ILuceneClient, DefaultLuceneClient>()
            .AddSingleton<ILuceneTaskLogger, DefaultLuceneTaskLogger>()
            .AddSingleton<ILuceneTaskProcessor, DefaultLuceneTaskProcessor>()
            .AddSingleton<ILuceneConfigurationStorageService, DefaultLuceneConfigurationStorageService>()
            .AddSingleton<ILuceneIndexService, DefaultLuceneIndexService>()
            .AddSingleton<ILuceneSearchService, DefaultLuceneSearchService>()
            .AddSingleton<ILuceneIndexManager, DefaultLuceneIndexManager>()
            .AddTransient<DefaultLuceneIndexingStrategy>();
}


public interface ILuceneBuilder
{
    /// <summary>
    /// Registers the given <typeparamref name="TStrategy" /> as a transient service under <paramref name="strategyName" />
    /// </summary>
    /// <typeparam name="TStrategy">The custom type of <see cref="ILuceneIndexingStrategy"/> </typeparam>
    /// <param name="strategyName">Used internally <typeparamref name="TStrategy" /> to enable dynamic assignment of strategies to search indexes. Names must be unique.</param>
    /// <exception cref="ArgumentException">
    ///     Thrown if a strategy has already been registered with the given <paramref name="strategyName"/>
    /// </exception>
    /// <returns>Returns this instance of <see cref="ILuceneBuilder"/>, allowing for further configuration in a fluent manner.</returns>
    ILuceneBuilder RegisterStrategy<TStrategy>(string strategyName) where TStrategy : class, ILuceneIndexingStrategy;


    /// <summary>
    /// Registers the given <see cref="Analyzer"/> <typeparamref name="TAnalyzer"/>  and
    /// as a selectable analyzer in the Admin UI
    /// </summary>
    /// <typeparam name="TAnalyzer">The type of <see cref="Analyzer"/> </typeparam>
    /// <param name="analyzerName">Used internally <typeparamref name="TAnalyzer"/> to enable dynamic assignment of analyzers to search indexes. Names must be unique.</param>
    /// <exception cref="ArgumentException">
    ///     Thrown if an analyzer has already been registered with the given <paramref name="analyzerName"/>
    /// </exception>
    /// <returns>Returns this instance of <see cref="ILuceneBuilder"/>, allowing for further configuration in a fluent manner.</returns>
    ILuceneBuilder RegisterAnalyzer<TAnalyzer>(string analyzerName) where TAnalyzer : Analyzer;


    /// <summary>
    /// Sets the <see cref="LuceneVersion"/> lucene version which will be used by <see cref="Analyzer"/> for search indexes.
    /// Defaults to <c><see cref="LuceneVersion.LUCENE_48"/></c>
    /// </summary>
    /// <param name="matchVersion"><see cref="LuceneVersion"/> to be used by the <see cref="Analyzer"/></param>
    /// <returns>Returns this instance of <see cref="ILuceneBuilder"/>, allowing for further configuration in a fluent manner.</returns>
    ILuceneBuilder SetAnalyzerLuceneVersion(LuceneVersion matchVersion);
}


internal class LuceneBuilder : ILuceneBuilder
{
    private readonly IServiceCollection serviceCollection;

    /// <summary>
    /// If true, the <see cref="DefaultLuceneIndexingStrategy" /> will be available as an explicitly selectable indexing strategy
    /// within the Admin UI. Defaults to <c>true</c>
    /// </summary>
    public bool IncludeDefaultStrategy { get; set; } = true;

    /// <summary>
    /// If true, the <see cref="StandardAnalyzer" /> will be available as an explicitly selectable analyzer
    /// within the Admin UI. Defaults to <c>true</c>
    /// </summary>
    public bool IncludeDefaultAnalyzer { get; set; } = true;

    public LuceneBuilder(IServiceCollection serviceCollection) => this.serviceCollection = serviceCollection;


    /// <summary>
    /// Registers the <see cref="ILuceneIndexingStrategy"/> strategy <typeparamref name="TStrategy" /> in DI and
    /// as a selectable strategy in the Admin UI
    /// </summary>
    /// <typeparam name="TStrategy">The custom type of <see cref="ILuceneIndexingStrategy"/> </typeparam>
    /// <param name="strategyName">Used internally <typeparamref name="TStrategy" /> to enable dynamic assignment of strategies to search indexes. Names must be unique.</param>
    /// <returns>Returns this instance of <see cref="ILuceneBuilder"/>, allowing for further configuration in a fluent manner.</returns>
    public ILuceneBuilder RegisterStrategy<TStrategy>(string strategyName) where TStrategy : class, ILuceneIndexingStrategy
    {
        StrategyStorage.AddStrategy<TStrategy>(strategyName);
        serviceCollection.AddTransient<TStrategy>();

        return this;
    }


    /// <summary>
    /// Registers the <see cref="Analyzer"/> analyzer <typeparamref name="TAnalyzer"/>
    /// as a selectable analyzer in the Admin UI. When selected this analyzer will be used to process indexed items.
    /// </summary>
    /// <typeparam name="TAnalyzer">The type of <see cref="Analyzer"/> </typeparam>
    /// <param name="analyzerName">Used internally <typeparamref name="TAnalyzer"/> to enable dynamic assignment of analyzers to search indexes. Names must be unique.</param>
    /// <returns>Returns this instance of <see cref="ILuceneBuilder"/>, allowing for further configuration in a fluent manner.</returns>
    public ILuceneBuilder RegisterAnalyzer<TAnalyzer>(string analyzerName) where TAnalyzer : Analyzer
    {
        AnalyzerStorage.AddAnalyzer<TAnalyzer>(analyzerName);

        return this;
    }


    /// <summary>
    /// Sets the <see cref="LuceneVersion"/> lucene version which will be used by <see cref="Analyzer"/> for indexing.
    /// Defaults to <c><see cref="LuceneVersion.LUCENE_48"/></c>
    /// </summary>
    /// <param name="matchVersion"><see cref="LuceneVersion"/> to be used by the <see cref="Analyzer"/></param>
    /// <returns>Returns this instance of <see cref="ILuceneBuilder"/>, allowing for further configuration in a fluent manner.</returns>
    public ILuceneBuilder SetAnalyzerLuceneVersion(LuceneVersion matchVersion)
    {
        AnalyzerStorage.SetAnalyzerLuceneVersion(matchVersion);

        return this;
    }
}
