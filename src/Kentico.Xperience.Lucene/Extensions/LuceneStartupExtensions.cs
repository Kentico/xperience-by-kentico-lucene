using System.Runtime.CompilerServices;
using Kentico.Xperience.Lucene;
using Kentico.Xperience.Lucene.Services;

[assembly: InternalsVisibleTo("Kentico.Xperience.Lucene.Tests")]

namespace Microsoft.Extensions.DependencyInjection;

public static class LuceneStartupExtensions
{
    /// <summary>
    /// Adds Lucene services and custom module to application using the <see cref="DefaultLuceneIndexingStrategy"/> for all indexes
    /// </summary>
    /// <param name="serviceCollection"></param>
    /// <returns></returns>
    public static IServiceCollection AddLucene(this IServiceCollection serviceCollection)
    {
        serviceCollection
            .AddSingleton<LuceneModuleInstaller>()
            .AddSingleton<ILuceneClient, DefaultLuceneClient>()
            .AddSingleton<ILuceneTaskLogger, DefaultLuceneTaskLogger>()
            .AddSingleton<ILuceneTaskProcessor, DefaultLuceneTaskProcessor>()
            .AddSingleton<IConfigurationStorageService, DefaultConfigurationStorageService>()
            .AddSingleton<ILuceneIndexService, DefaultLuceneIndexService>();

        return serviceCollection;
    }

    /// <summary>
    /// Adds Lucene services and custom module to application with customized options provided by the <see cref="ILuceneBuilder"/>
    /// in the <paramref name="configure" /> action.
    /// </summary>
    /// <param name="serviceCollection"></param>
    /// <param name="configure"></param>
    /// <returns></returns>
    public static IServiceCollection AddLucene(this IServiceCollection serviceCollection, Action<ILuceneBuilder> configure)
    {
        serviceCollection.AddLucene();

        var builder = new LuceneBuilder(serviceCollection);

        configure(builder);

        return serviceCollection;
    }
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

    public LuceneBuilder(IServiceCollection serviceCollection) => this.serviceCollection = serviceCollection;

    public ILuceneBuilder RegisterStrategy<TStrategy>(string strategyName) where TStrategy : class, ILuceneIndexingStrategy
    {
        StrategyStorage.AddStrategy<TStrategy>(strategyName);
        serviceCollection.AddTransient<TStrategy>();

        return this;
    }
}
