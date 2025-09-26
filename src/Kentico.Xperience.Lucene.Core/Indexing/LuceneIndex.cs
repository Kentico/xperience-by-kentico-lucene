using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Util;

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
    [Obsolete("This property is no longer used. WebsiteChannelName is now used within Kentico.Xperience.Lucene.Core.Indexing.LuceneIndexChannelConfiguration and is a property of each channel configuration.")]
    public string WebSiteChannelName { get; } = string.Empty;


    /// <summary>
    /// The Language used on the WebSite on the Channel which is indexed.
    /// </summary>
    public List<string> LanguageNames { get; }


    /// <summary>
    /// A list of reusable content types that will be indexed.
    /// </summary>
    public List<string> IncludedReusableContentTypes { get; }


    /// <summary>
    /// Lucene Analyzer used for indexing.
    /// </summary>
    public Analyzer LuceneAnalyzer { get; }


    /// <summary>
    /// The type of the class which extends <see cref="ILuceneIndexingStrategy"/>.
    /// </summary>
    public Type LuceneIndexingStrategyType { get; }


    /// <summary>
    /// Index storage context, employs picked storage strategy
    /// </summary>
    public IndexStorageContext StorageContext { get; }


    internal IEnumerable<LuceneIndexChannelConfiguration> ChannelConfigurations { get; set; }


    internal LuceneIndex(LuceneIndexModel indexConfiguration, Dictionary<string, Type> strategies, Dictionary<string, Type> analyzers, LuceneVersion matchVersion)
    {
        Identifier = indexConfiguration.Id;
        IndexName = indexConfiguration.IndexName;
        LanguageNames = indexConfiguration.LanguageNames.ToList();
        IncludedReusableContentTypes = indexConfiguration.ReusableContentTypeNames.ToList();
        ChannelConfigurations = indexConfiguration.Channels.ToList();

        var strategy = typeof(DefaultLuceneIndexingStrategy);

        if (strategies.ContainsKey(indexConfiguration.StrategyName))
        {
            strategy = strategies[indexConfiguration.StrategyName];
        }

        var analyzerType = typeof(StandardAnalyzer);

        if (analyzers.ContainsKey(indexConfiguration.AnalyzerName))
        {
            analyzerType = analyzers[indexConfiguration.AnalyzerName];
        }

        var constructorParameters = analyzerType.GetConstructors().Select(x => new
        {
            Constructor = x,
            Parameters = x.GetParameters()
        });
        var constructor = constructorParameters.First(x => x.Parameters.Length == 1 && x.Parameters.Single().ParameterType == typeof(LuceneVersion)).Constructor;
        LuceneAnalyzer = (Analyzer)constructor.Invoke([matchVersion]);

        LuceneIndexingStrategyType = strategy;

        string indexStoragePath = Path.Combine(Environment.CurrentDirectory, "App_Data", "LuceneSearch", indexConfiguration.IndexName);
        StorageContext = new IndexStorageContext(new GenerationStorageStrategy(), indexStoragePath, new IndexRetentionPolicy(0));
    }
}
