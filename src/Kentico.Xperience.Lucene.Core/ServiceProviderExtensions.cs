using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;

using Kentico.Xperience.Lucene.Core.Indexing;

namespace Microsoft.Extensions.DependencyInjection;

internal static class ServiceProviderExtensions
{
    /// <summary>
    /// Returns an instance of the <see cref="ILuceneIndexingStrategy"/> assigned to the given <paramref name="index" />.
    /// Used to generate instances of a <see cref="ILuceneIndexingStrategy"/> service type that can change at runtime.
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="index"></param>
    /// <exception cref="InvalidOperationException">
    ///     Thrown if the assigned <see cref="ILuceneIndexingStrategy"/> cannot be instantiated.
    ///     This shouldn't normally occur because we fallback to <see cref="DefaultLuceneIndexingStrategy" /> if no custom strategy is specified.
    ///     However, incorrect dependency management in user-code could cause issues.
    /// </exception>
    /// <returns></returns>
    public static ILuceneIndexingStrategy GetRequiredStrategy(this IServiceProvider serviceProvider, LuceneIndex index)
    {
        var strategy = serviceProvider.GetRequiredService(index.LuceneIndexingStrategyType) as ILuceneIndexingStrategy;

        return strategy!;
    }

    /// <summary>
    /// Returns an instance of the <see cref="Analyzer"/> assigned to the given <paramref name="index" />.
    /// Used to generate instances of a <see cref="Analyzer"/> service type that can change at runtime.
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="index"></param>
    /// <exception cref="InvalidOperationException">
    ///     Thrown if the assigned <see cref="Analyzer"/> cannot be instantiated.
    ///     This shouldn't normally occur because we fallback to <see cref="StandardAnalyzer" /> if no custom analyzer is specified.
    ///     However, incorrect dependency management in user-code could cause issues.
    /// </exception>
    /// <returns></returns>
    public static Analyzer GetRequiredAnalyzer(this IServiceProvider serviceProvider, LuceneIndex index)
    {
        var analyzer = serviceProvider.GetRequiredService(index.LuceneAnalyzerType) as Analyzer;

        return analyzer!;
    }
}
