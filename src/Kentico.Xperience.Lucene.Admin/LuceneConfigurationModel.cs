using System.ComponentModel.DataAnnotations;

using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using Kentico.Xperience.Lucene.Admin.Providers;
using Kentico.Xperience.Lucene.Core.Indexing;

namespace Kentico.Xperience.Lucene.Admin;

public class LuceneConfigurationModel
{
    public int Id { get; set; }

    [TextInputComponent(
        Label = "Index Name",
        ExplanationText = "Changing this value on an existing index without changing application code will cause the search experience to stop working.",
        Order = 1)]
    [Required]
    [MinLength(1)]
    public string IndexName { get; set; } = "";

    [GeneralSelectorComponent(dataProviderType: typeof(LanguageOptionsProvider), Label = "Indexed Languages", Order = 2)]
    public IEnumerable<string> LanguageNames { get; set; } = Enumerable.Empty<string>();

    [DropDownComponent(Label = "Channel Name", DataProviderType = typeof(ChannelOptionsProvider), Order = 3)]
    public string ChannelName { get; set; } = "";

    [DropDownComponent(Label = "Indexing Strategy", DataProviderType = typeof(IndexingStrategyOptionsProvider), Order = 4)]
    public string StrategyName { get; set; } = "";

    [DropDownComponent(Label = "Lucene Analyzer", DataProviderType = typeof(AnalyzerOptionsProvider), Order = 5)]
    public string AnalyzerName { get; set; } = "";

    [TextInputComponent(Label = "Rebuild Hook")]
    public string RebuildHook { get; set; } = "";

    [LuceneIndexConfigurationComponent(Label = "Included Paths")]
    public IEnumerable<LuceneIndexIncludedPath> Paths { get; set; } = Enumerable.Empty<LuceneIndexIncludedPath>();

    public LuceneConfigurationModel() { }

    public LuceneConfigurationModel(
        LuceneIndexModel luceneModel
    )
    {
        Id = luceneModel.Id;
        IndexName = luceneModel.IndexName;
        LanguageNames = luceneModel.LanguageNames;
        ChannelName = luceneModel.ChannelName;
        StrategyName = luceneModel.StrategyName;
        AnalyzerName = luceneModel.AnalyzerName;
        RebuildHook = luceneModel.RebuildHook;
        Paths = luceneModel.Paths;
    }

    public LuceneIndexModel ToLuceneModel() =>
        new()
        {
            Id = Id,
            IndexName = IndexName,
            LanguageNames = LanguageNames,
            ChannelName = ChannelName,
            AnalyzerName = AnalyzerName,
            StrategyName = StrategyName,
            RebuildHook = RebuildHook,
            Paths = Paths
        };
}
