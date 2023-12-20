using Kentico.Xperience.Lucene;
using Kentico.Xperience.Lucene.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Application startup extension methods.
/// </summary>
public static class LuceneStartupExtensions
{
    public static IServiceCollection AddLucene(this IServiceCollection serviceCollection)
    {
        serviceCollection.TryAddSingleton<LuceneModuleInstaller>();

        return serviceCollection;
    }

    public static IServiceCollection RegisterStrategy<TStrategy>(this IServiceCollection serviceCollection, string strategyName) where TStrategy : ILuceneIndexingStrategy, new()
    {
        StrategyStorage.AddStrategy<TStrategy>(strategyName);
        return serviceCollection;
    }
}
