using Lucene.Net.Analysis.Standard;

namespace Kentico.Xperience.Lucene.Core.Indexing;

/// <summary>
/// Represents the configuration of an Lucene index.
/// </summary>
public sealed class LuceneIndex
{
    /// <summary>
    /// An arbitrary ID used to identify the Lucene index in the admin UI.
    /// </summary>
    public int Identifier { get; set; }

    /// <summary>
    /// The code name of the Lucene index.
    /// </summary>
    public string IndexName { get; }

    /// <summary>
    /// The Name of the WebSiteChannel.
    /// </summary>
    public string WebSiteChannelName { get; }

    /// <summary>
    /// The Language used on the WebSite on the Channel which is indexed.
    /// </summary>
    public List<string> LanguageNames { get; }

    /// <summary>
    /// The type of Lucene Analyzer which extends <see cref="LuceneAnalyzerType"/>.
    /// </summary>
    public Type LuceneAnalyzerType { get; }

    /// <summary>
    /// The type of the class which extends <see cref="ILuceneIndexingStrategy"/>.
    /// </summary>
    public Type LuceneIndexingStrategyType { get; }

    /// <summary>
    /// Index storage context, employs picked storage strategy
    /// </summary>
    public IndexStorageContext StorageContext { get; }

    internal IEnumerable<LuceneIndexIncludedPath> IncludedPaths { get; set; }

    internal LuceneIndex(LuceneIndexModel indexConfiguration, Dictionary<string, Type> strategies, Dictionary<string, Type> analyzers)
    {
        Identifier = indexConfiguration.Id;
        IndexName = indexConfiguration.IndexName;
        WebSiteChannelName = indexConfiguration.ChannelName;
        LanguageNames = indexConfiguration.LanguageNames.ToList();
        IncludedPaths = indexConfiguration.Paths;

        var strategy = typeof(DefaultLuceneIndexingStrategy);

        if (strategies.ContainsKey(indexConfiguration.StrategyName))
        {
            strategy = strategies[indexConfiguration.StrategyName];
        }

        var analyzer = typeof(StandardAnalyzer);

        if (analyzers.ContainsKey(indexConfiguration.AnalyzerName))
        {
            analyzer = analyzers[indexConfiguration.AnalyzerName];
        }

        LuceneIndexingStrategyType = strategy;
        LuceneAnalyzerType = analyzer;

        string indexStoragePath = Path.Combine(Environment.CurrentDirectory, "App_Data", "LuceneSearch", indexConfiguration.IndexName);
        StorageContext = new IndexStorageContext(new GenerationStorageStrategy(), indexStoragePath, new IndexRetentionPolicy(4));
    }
}
