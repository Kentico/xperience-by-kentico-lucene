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

    [LuceneIndexConfigurationComponent(Label = "Included Paths", Order = 2)]
    public IEnumerable<LuceneIndexIncludedPath> Paths { get; set; } = Enumerable.Empty<LuceneIndexIncludedPath>();

    [GeneralSelectorComponent(dataProviderType: typeof(ReusableContentOptionsProvider), Label = "Included Reusable Content Types", Order = 3)]
    public IEnumerable<string> ReusableContentTypeNames { get; set; } = Enumerable.Empty<string>();

    [GeneralSelectorComponent(dataProviderType: typeof(LanguageOptionsProvider), Label = "Indexed Languages", Order = 4)]
    public IEnumerable<string> LanguageNames { get; set; } = Enumerable.Empty<string>();

    [DropDownComponent(Label = "Channel Name", DataProviderType = typeof(ChannelOptionsProvider), Order = 5)]
    public string ChannelName { get; set; } = "";

    [DropDownComponent(Label = "Indexing Strategy", DataProviderType = typeof(IndexingStrategyOptionsProvider), Order = 6)]
    public string StrategyName { get; set; } = "";

    [DropDownComponent(Label = "Lucene Analyzer", DataProviderType = typeof(AnalyzerOptionsProvider), Order = 7)]
    public string AnalyzerName { get; set; } = "";

    [TextInputComponent(Label = "Rebuild Hook", Order = 8)]
    public string RebuildHook { get; set; } = "";

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
        ReusableContentTypeNames = luceneModel.ReusableContentTypeNames;
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
            Paths = Paths,
            ReusableContentTypeNames = ReusableContentTypeNames
        };
}
