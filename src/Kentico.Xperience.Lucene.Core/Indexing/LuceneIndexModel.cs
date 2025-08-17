using CMS.ContentEngine;

namespace Kentico.Xperience.Lucene.Core.Indexing;

public class LuceneIndexModel
{
    public int Id { get; set; }

    public string IndexName { get; set; } = "";

    public IEnumerable<string> LanguageNames { get; set; } = Enumerable.Empty<string>();

    public string ChannelName { get; set; } = "";

    public string StrategyName { get; set; } = "";

    public string AnalyzerName { get; set; } = "";

    public string RebuildHook { get; set; } = "";

    public IEnumerable<LuceneIndexIncludedPath> Paths { get; set; } = Enumerable.Empty<LuceneIndexIncludedPath>();

    public IEnumerable<LuceneIndexChannelConfiguration> Channels { get; set; } = Enumerable.Empty<LuceneIndexChannelConfiguration>();

    public IEnumerable<string> ReusableContentTypeNames { get; set; } = Enumerable.Empty<string>();

    public LuceneIndexModel() { }

    public LuceneIndexModel(
        LuceneIndexItemInfo index,
        IEnumerable<LuceneIndexLanguageItemInfo> indexLanguages,
        IEnumerable<LuceneIncludedPathItemInfo> indexPaths,
        IEnumerable<LuceneIndexContentType> contentTypes,
        IEnumerable<LuceneReusableContentTypeItemInfo> reusableContentTypes,
        IEnumerable<ChannelInfo> channelInfos
    )
    {
        Id = index.LuceneIndexItemId;
        IndexName = index.LuceneIndexItemIndexName;
        RebuildHook = index.LuceneIndexItemRebuildHook;
        StrategyName = index.LuceneIndexItemStrategyName;
        AnalyzerName = index.LuceneIndexItemAnalyzerName;
        LanguageNames = indexLanguages
            .Where(l => l.LuceneIndexLanguageItemIndexItemId == index.LuceneIndexItemId)
            .Select(l => l.LuceneIndexLanguageItemName)
            .ToList();
        ReusableContentTypeNames = reusableContentTypes
            .Where(c => c.LuceneReusableContentTypeItemIndexItemId == index.LuceneIndexItemId)
            .Select(c => c.LuceneReusableContentTypeItemContentTypeName)
            .ToList();
        Paths = indexPaths
            .Where(p => p.LuceneIncludedPathItemIndexItemId == index.LuceneIndexItemId)
            .Select(p => new LuceneIndexIncludedPath(p,
                contentTypes.Where(x => x.LucenePathItemId == p.LuceneIncludedPathItemId))
            )
            .ToList();
        Channels = indexPaths
            .Where(p => p.LuceneIncludedPathItemIndexItemId == index.LuceneIndexItemId)
            .GroupBy(x => x.LuceneIncludedPathItemChannelName)
            .Select(x => new LuceneIndexChannelConfiguration(x.ToList(), contentTypes, channelInfos))
            .ToList();
    }
}
