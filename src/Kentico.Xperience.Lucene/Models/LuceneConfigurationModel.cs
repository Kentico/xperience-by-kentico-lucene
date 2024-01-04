using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using Kentico.Xperience.Lucene.Admin.Components;
using Kentico.Xperience.Lucene.Admin.Providers;

namespace Kentico.Xperience.Lucene.Models;

public class LuceneConfigurationModel
{
    public int Id { get; set; }

    [TextInputComponent(Label = "Index Name", Order = 1)]
    public string? IndexName { get; set; }

    [GeneralSelectorComponent(dataProviderType: typeof(LanguageOptionsProvider), Label = "Indexed Languages", Order = 2)]
    public IEnumerable<string>? LanguageNames { get; set; }

    [DropDownComponent(Label = "Channel Name", DataProviderType = typeof(ChannelOptionsProvider), Order = 3)]
    public string? ChannelName { get; set; }

    [DropDownComponent(Label = "Indexing Strategy", DataProviderType = typeof(IndexingStrategyOptionsProvider), Order = 4)]
    public string? StrategyName { get; set; }

    [TextInputComponent(Label = "Rebuild Hook")]
    public string? RebuildHook { get; set; }

    [ListComponent(Label = "Included Paths")]
    public List<IncludedPath>? Paths { get; set; }
}
