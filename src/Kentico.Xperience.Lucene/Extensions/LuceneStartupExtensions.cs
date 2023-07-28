using Kentico.Xperience.Lucene.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Kentico.Xperience.Lucene.Extensions
{
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
            if (indexes != null)
            {
                Array.ForEach(indexes, index => IndexStore.Instance.AddIndex(index));
            }
            return services;
        }
    }
}
