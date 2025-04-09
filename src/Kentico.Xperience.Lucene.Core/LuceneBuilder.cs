using Kentico.Xperience.Lucene.Core.Indexing;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis;
using Lucene.Net.Util;
using Microsoft.Extensions.DependencyInjection;

namespace Kentico.Xperience.Lucene.Core;
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
