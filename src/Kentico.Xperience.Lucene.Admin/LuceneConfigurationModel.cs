using System.ComponentModel.DataAnnotations;

using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using Kentico.Xperience.Lucene.Admin.Providers;
using Kentico.Xperience.Lucene.Core.Indexing;

namespace Kentico.Xperience.Lucene.Admin;

/// <summary>
/// Represents the configuration model for a Lucene index.
/// </summary>
public sealed class LuceneConfigurationModel
{
    /// <summary>
    /// The unique identifier.
    /// </summary>
    public int Id { get; set; }


    /// <summary>
    /// The name of the index used for search operations.
    /// </summary>
    [TextInputComponent(
        Label = "Index Name",
        ExplanationText = "Changing this value on an existing index without changing application code will cause the search experience to stop working.",
        Order = 1)]
    [Required]
    [MinLength(1)]
    public string IndexName { get; set; } = "";


    [Obsolete("The property is not used anymore. Use WebsiteChannelName on items of the Channels collection instead.")]
    public string ChannelName { get; set; } = "";


    [Obsolete("The property is not used anymore. Use IncludedPaths on items of the Channels collection instead.")]
    public IEnumerable<LuceneIndexIncludedPath> Paths { get; set; } = Enumerable.Empty<LuceneIndexIncludedPath>();


    /// <summary>
    /// The configurations for different website channels associated with the index.
    /// </summary>
    [LuceneSearchIndexConfigurationComponent(Label = "Configured Channels", Order = 2)]
    public IEnumerable<LuceneIndexChannelConfiguration> Channels { get; set; } = Enumerable.Empty<LuceneIndexChannelConfiguration>();


    /// <summary>
    /// The reusable content types that the index will include.
    /// </summary>
    [GeneralSelectorComponent(dataProviderType: typeof(ReusableContentOptionsProvider), Label = "Included Reusable Content Types", Order = 3)]
    public IEnumerable<string> ReusableContentTypeNames { get; set; } = Enumerable.Empty<string>();


    /// <summary>
    /// The languages that the index will support.
    /// </summary>
    [GeneralSelectorComponent(dataProviderType: typeof(LanguageOptionsProvider), Label = "Indexed Languages", Order = 4)]
    public IEnumerable<string> LanguageNames { get; set; } = Enumerable.Empty<string>();


    /// <summary>
    /// The name of the indexing strategy to be used for determining how and when items are indexed.
    /// </summary>
    [DropDownComponent(Label = "Indexing Strategy", DataProviderType = typeof(IndexingStrategyOptionsProvider), Order = 6)]
    public string StrategyName { get; set; } = "";


    /// <summary>
    /// The name of the Lucene analyzer to be used for text analysis during indexing and searching.
    /// </summary>
    [DropDownComponent(Label = "Lucene Analyzer", DataProviderType = typeof(AnalyzerOptionsProvider), Order = 7)]
    public string AnalyzerName { get; set; } = "";


    /// <summary>
    /// The rebuild hook of the index.
    /// </summary>
    [TextInputComponent(Label = "Rebuild Hook", Order = 8)]
    public string RebuildHook { get; set; } = "";


    /// <summary>
    /// An empty constructor for model binding.
    /// </summary>
    public LuceneConfigurationModel() { }


    /// <summary>
    /// Initializes a new instance of the <see cref="LuceneConfigurationModel"/> class based on an existing <see cref="LuceneIndexModel"/>.
    /// </summary>
    /// <returns><see cref="LuceneConfigurationModel"/></returns>
    public LuceneConfigurationModel(
        LuceneIndexModel luceneModel
    )
    {
        Id = luceneModel.Id;
        IndexName = luceneModel.IndexName;
        LanguageNames = luceneModel.LanguageNames;
        StrategyName = luceneModel.StrategyName;
        AnalyzerName = luceneModel.AnalyzerName;
        RebuildHook = luceneModel.RebuildHook;
#pragma warning disable CS0618 // Type or member is obsolete
        Paths = luceneModel.Paths;
        ChannelName = luceneModel.ChannelName;
#pragma warning restore CS0618 // Type or member is obsolete
        Channels = luceneModel.Channels;
        ReusableContentTypeNames = luceneModel.ReusableContentTypeNames;
    }


    /// <summary>
    /// Initializes a new instance of the <see cref="LuceneIndexModel"/> class based on the current configuration.
    /// </summary>
    /// <returns><see cref="LuceneIndexModel"/>.</returns>
    public LuceneIndexModel ToLuceneModel() =>
        new()
        {
            Id = Id,
            IndexName = IndexName,
            LanguageNames = LanguageNames,
            AnalyzerName = AnalyzerName,
            StrategyName = StrategyName,
            RebuildHook = RebuildHook,
            Channels = Channels,
            ReusableContentTypeNames = ReusableContentTypeNames
        };
}
