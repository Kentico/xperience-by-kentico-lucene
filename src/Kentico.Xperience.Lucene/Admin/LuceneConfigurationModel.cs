using System.ComponentModel.DataAnnotations;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;

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

    [TextInputComponent(Label = "Rebuild Hook")]
    public string RebuildHook { get; set; } = "";

    [LuceneIndexConfigurationComponent(Label = "Included Paths")]
    public IEnumerable<LuceneIndexIncludedPath> Paths { get; set; } = Enumerable.Empty<LuceneIndexIncludedPath>();

    public LuceneConfigurationModel() { }

    public LuceneConfigurationModel(
        LuceneIndexItemInfo index,
        IEnumerable<LuceneIndexLanguageItemInfo> indexLanguages,
        IEnumerable<LuceneIncludedPathItemInfo> indexPaths,
        IEnumerable<LuceneIndexContentType> contentTypes
    )
    {
        Id = index.LuceneIndexItemId;
        IndexName = index.LuceneIndexItemIndexName;
        ChannelName = index.LuceneIndexItemChannelName;
        RebuildHook = index.LuceneIndexItemRebuildHook;
        StrategyName = index.LuceneIndexItemStrategyName;
        LanguageNames = indexLanguages
            .Where(l => l.LuceneIndexLanguageItemIndexItemId == index.LuceneIndexItemId)
            .Select(l => l.LuceneIndexLanguageItemName)
            .ToList();
        Paths = indexPaths
            .Where(p => p.LuceneIncludedPathItemIndexItemId == index.LuceneIndexItemId)
            .Select(p => new LuceneIndexIncludedPath(p, contentTypes))
            .ToList();
    }
}
