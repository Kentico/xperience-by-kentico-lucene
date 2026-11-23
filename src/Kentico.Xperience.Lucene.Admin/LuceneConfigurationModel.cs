using System.ComponentModel.DataAnnotations;

using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using Kentico.Xperience.Lucene.Admin.Providers;
using Kentico.Xperience.Lucene.Core.Indexing;

namespace Kentico.Xperience.Lucene.Admin;

/// <summary>
/// Represents the configuration model for a Lucene index.
/// </summary>
[FormCategory(Label = "Identity", Order = 0, Collapsible = false)]
[FormCategory(Label = "Website channel content", Order = 10, Collapsible = false)]
[FormCategory(Label = "Reusable content", Order = 20, Collapsible = false)]
[FormCategory(Label = "Languages and analysis", Order = 30, Collapsible = false)]
[FormCategory(Label = "Indexing", Order = 40, Collapsible = false)]
public sealed class LuceneConfigurationModel
{
    /// <summary>
    /// The unique identifier.
    /// </summary>
    public int Id { get; set; }


    /// <summary>
    /// The name shown in the administration. Falls back to the index code name when empty.
    /// </summary>
    [TextInputComponent(
        Label = "Display Name",
        ExplanationText = "Shown in the administration only. Can be changed at any time.",
        Order = 1)]
    public string DisplayName { get; set; } = "";


    /// <summary>
    /// The code name of the index used for search operations. Fixed after the index is created.
    /// </summary>
    [TextInputComponent(
        Label = "Code Name",
        ExplanationText = "The code name used by application code to find the index. It cannot be changed after creation.",
        EditMode = FormEditMode.Disabled,
        Order = 2)]
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
    [LuceneSearchIndexConfigurationComponent(Label = "Website Channels", ExplanationText = "Pages of the selected content types under each path are indexed.", Order = 11)]
    public IEnumerable<LuceneIndexChannelConfiguration> Channels { get; set; } = Enumerable.Empty<LuceneIndexChannelConfiguration>();


    /// <summary>
    /// The reusable content types that the index will include.
    /// </summary>
    [GeneralSelectorComponent(dataProviderType: typeof(ReusableContentOptionsProvider), Label = "Reusable Content Types", ExplanationText = "Reusable content items of these types are indexed.", Order = 21)]
    public IEnumerable<string> ReusableContentTypeNames { get; set; } = Enumerable.Empty<string>();


    /// <summary>
    /// The languages that the index will support.
    /// </summary>
    [GeneralSelectorComponent(dataProviderType: typeof(LanguageOptionsProvider), Label = "Indexed Languages", Order = 31)]
    public IEnumerable<string> LanguageNames { get; set; } = Enumerable.Empty<string>();


    /// <summary>
    /// The name of the indexing strategy to be used for determining how and when items are indexed.
    /// </summary>
    [DropDownComponent(Label = "Indexing Strategy", DataProviderType = typeof(IndexingStrategyOptionsProvider), Order = 41)]
    public string StrategyName { get; set; } = "";


    /// <summary>
    /// The name of the Lucene analyzer to be used for text analysis during indexing and searching.
    /// </summary>
    [DropDownComponent(Label = "Lucene Analyzer", DataProviderType = typeof(AnalyzerOptionsProvider), Order = 32)]
    public string AnalyzerName { get; set; } = "";


    /// <summary>
    /// The rebuild hook of the index.
    /// </summary>
    [TextInputComponent(Label = "Rebuild Hook", Order = 42)]
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
        DisplayName = luceneModel.DisplayName;
        LanguageNames = luceneModel.LanguageNames;
        StrategyName = ResolveOption(luceneModel.StrategyName, StrategyStorage.Strategies.Keys);
        AnalyzerName = ResolveOption(luceneModel.AnalyzerName, AnalyzerStorage.Analyzers.Keys);
        RebuildHook = luceneModel.RebuildHook;
#pragma warning disable CS0618 // Type or member is obsolete
        Paths = luceneModel.Paths;
        ChannelName = luceneModel.ChannelName;
#pragma warning restore CS0618 // Type or member is obsolete
        Channels = luceneModel.Channels;
        ReusableContentTypeNames = luceneModel.ReusableContentTypeNames;
    }


    // A blank stored option is not in the dropdown, so the form would select the first option and report a change nobody made.
    private static string ResolveOption(string stored, IEnumerable<string> options)
    {
        if (!string.IsNullOrWhiteSpace(stored))
        {
            return stored;
        }

        var all = options.ToList();

        return all.Find(o => o.Contains("Standard", StringComparison.OrdinalIgnoreCase)) ?? all.FirstOrDefault() ?? "";
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
            DisplayName = DisplayName,
            LanguageNames = LanguageNames,
            AnalyzerName = AnalyzerName,
            StrategyName = StrategyName,
            RebuildHook = RebuildHook,
#pragma warning disable CS0618 // Type or member is obsolete
            Paths = Paths,
            ChannelName = ChannelName,
#pragma warning restore CS0618 // Type or member is obsolete
            Channels = Channels,
            ReusableContentTypeNames = ReusableContentTypeNames
        };
}
