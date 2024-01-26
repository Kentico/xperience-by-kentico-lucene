using System.Runtime.CompilerServices;
using Kentico.Xperience.Lucene.Admin;
using Kentico.Xperience.Lucene.Indexing;
using Kentico.Xperience.Lucene.Search;

[assembly: InternalsVisibleTo("Kentico.Xperience.Lucene.Tests")]

namespace Microsoft.Extensions.DependencyInjection;

public static class LuceneStartupExtensions
{
    /// <summary>
    /// Adds Lucene services and custom module to application using the <see cref="DefaultLuceneIndexingStrategy"/> for all indexes
    /// </summary>
    /// <param name="serviceCollection"></param>
    /// <returns></returns>
    public static IServiceCollection AddKenticoLucene(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddLuceneServicesInternal();

        StrategyStorage.AddStrategy<DefaultLuceneIndexingStrategy>("Default");

        return serviceCollection;
    }

    /// <summary>
    /// Adds Lucene services and custom module to application with customized options provided by the <see cref="ILuceneBuilder"/>
    /// in the <paramref name="configure" /> action.
    /// </summary>
    /// <param name="serviceCollection"></param>
    /// <param name="configure"></param>
    /// <returns></returns>
    public static IServiceCollection AddKenticoLucene(this IServiceCollection serviceCollection, Action<ILuceneBuilder> configure)
    {
        serviceCollection.AddLuceneServicesInternal();

        var builder = new LuceneBuilder(serviceCollection);

        configure(builder);

        if (builder.IncludeDefaultStrategy)
        {
            serviceCollection.AddTransient<DefaultLuceneIndexingStrategy>();
            builder.RegisterStrategy<DefaultLuceneIndexingStrategy>("Default");
        }

        return serviceCollection;
    }

    /// <summary>
    /// Adds Lucene services and custom module to application using the <see cref="DefaultLuceneIndexingStrategy"/> for all indexes
    /// </summary>
    /// <param name="serviceCollection"></param>
    /// <returns></returns>
    [Obsolete("Will be removed in next major version. Use .AddKenticoLucene() instead.")]
    public static IServiceCollection AddLucene(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddLuceneServicesInternal();

        StrategyStorage.AddStrategy<DefaultLuceneIndexingStrategy>("Default");

        return serviceCollection;
    }

    /// <summary>
    /// Adds Lucene services and custom module to application with customized options provided by the <see cref="ILuceneBuilder"/>
    /// in the <paramref name="configure" /> action.
    /// </summary>
    /// <param name="serviceCollection"></param>
    /// <param name="configure"></param>
    /// <returns></returns>
    [Obsolete("Will be removed in next major version. Use .AddKenticoLucene() instead.")]
    public static IServiceCollection AddLucene(this IServiceCollection serviceCollection, Action<ILuceneBuilder> configure)
    {
        serviceCollection.AddLuceneServicesInternal();

        var builder = new LuceneBuilder(serviceCollection);

        configure(builder);

        if (builder.IncludeDefaultStrategy)
        {
            serviceCollection.AddTransient<DefaultLuceneIndexingStrategy>();
            builder.RegisterStrategy<DefaultLuceneIndexingStrategy>("Default");
        }

        return serviceCollection;
    }

    private static IServiceCollection AddLuceneServicesInternal(this IServiceCollection services) =>
        services
            .AddSingleton<LuceneModuleInstaller>()
            .AddSingleton<LuceneModuleMigrator>()
            .AddSingleton<ILuceneClient, DefaultLuceneClient>()
            .AddSingleton<ILuceneTaskLogger, DefaultLuceneTaskLogger>()
            .AddSingleton<ILuceneTaskProcessor, DefaultLuceneTaskProcessor>()
            .AddSingleton<ILuceneConfigurationStorageService, DefaultLuceneConfigurationStorageService>()
            .AddSingleton<ILuceneIndexService, DefaultLuceneIndexService>()
            .AddSingleton<ILuceneSearchService, DefaultLuceneSearchService>()
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
    ///     Thrown if an strategy has already been registered with the given <paramref name="strategyName"/>
    /// </exception>
    /// <returns></returns>
    ILuceneBuilder RegisterStrategy<TStrategy>(string strategyName) where TStrategy : class, ILuceneIndexingStrategy;
}

internal class LuceneBuilder : ILuceneBuilder
{
    private readonly IServiceCollection serviceCollection;

    /// <summary>
    /// If true, the <see cref="DefaultLuceneIndexingStrategy" /> will be available as an explicitly selectable indexing strategy
    /// within the Admin UI. Defaults to <c>true</c>
    /// </summary>
    public bool IncludeDefaultStrategy { get; set; } = true;

    public LuceneBuilder(IServiceCollection serviceCollection) => this.serviceCollection = serviceCollection;

    /// <summary>
    /// Registers the <see cref="ILuceneIndexingStrategy"/> strategy <typeparamref name="TStrategy" /> in DI and
    /// as a selectable strategy in the Admin UI
    /// </summary>
    /// <typeparam name="TStrategy"></typeparam>
    /// <param name="strategyName"></param>
    /// <returns></returns>
    public ILuceneBuilder RegisterStrategy<TStrategy>(string strategyName) where TStrategy : class, ILuceneIndexingStrategy
    {
        StrategyStorage.AddStrategy<TStrategy>(strategyName);
        serviceCollection.AddTransient<TStrategy>();

        return this;
    }
}
