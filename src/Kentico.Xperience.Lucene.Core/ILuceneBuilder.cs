using Kentico.Xperience.Lucene.Core.Indexing;

using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Util;

namespace Kentico.Xperience.Lucene.Core;

public interface ILuceneBuilder
{
    /// <summary>
    /// If true, the <see cref="DefaultLuceneIndexingStrategy" /> will be available as an explicitly selectable indexing strategy
    /// within the Admin UI. Defaults to <c>true</c>
    /// </summary>
    bool IncludeDefaultStrategy { get; set; }


    /// <summary>
    /// If true, the <see cref="StandardAnalyzer" /> will be available as an explicitly selectable analyzer
    /// within the Admin UI. Defaults to <c>true</c>
    /// </summary>
    bool IncludeDefaultAnalyzer { get; set; }


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
