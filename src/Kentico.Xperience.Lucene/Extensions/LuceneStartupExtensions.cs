using Kentico.Xperience.Lucene;
using Kentico.Xperience.Lucene.Models;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Application startup extension methods.
/// </summary>
public static class LuceneStartupExtensions
{
    /// <summary>
    /// Registers the provided <paramref name="indexes"/> with the <see cref="IndexStore"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="indexes">The Lucene indexes to register.</param>
    public static IServiceCollection AddLucene(this IServiceCollection services, LuceneIndex[] indexes)
    {
        ArgumentNullException.ThrowIfNull(indexes);

        Array.ForEach(indexes, IndexStore.Instance.AddIndex);

        return services;
    }
}
