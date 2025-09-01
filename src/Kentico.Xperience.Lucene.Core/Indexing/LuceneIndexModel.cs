using CMS.ContentEngine;
using CMS.DataEngine;

using Lucene.Net.Analysis;

namespace Kentico.Xperience.Lucene.Core.Indexing;

/// <summary>
/// The model representing a Lucene index, including its properties, associated languages, channels, and content types.
/// </summary>
public sealed class LuceneIndexModel
{
    /// <summary>
    /// The <see cref="LuceneIndexItemInfo.LuceneIndexItemId"/>.
    /// </summary>
    public int Id { get; set; }


    /// <summary>
    /// The <see cref="LuceneIndexItemInfo.LuceneIndexItemIndexName"/>.
    /// </summary>
    public string IndexName { get; set; } = "";


    /// <summary>
    /// The collection of <see cref="ContentLanguageInfo.ContentLanguageName"/> names associated with the index, derived from <see cref="LuceneIndexLanguageItemInfo.LuceneIndexLanguageItemName"/>.
    /// </summary>
    public IEnumerable<string> LanguageNames { get; set; } = Enumerable.Empty<string>();


    /// <summary>
    /// The name if the selected <see cref="ILuceneIndexingStrategy"/>.
    /// </summary>
    public string StrategyName { get; set; } = "";


    /// <summary>
    /// The name of the selected <see cref="Analyzer"/> analyzer.
    /// </summary>
    public string AnalyzerName { get; set; } = "";


    /// <summary>
    /// The <see cref="LuceneIndexItemInfo.LuceneIndexItemRebuildHook"/>.
    /// </summary>
    public string RebuildHook { get; set; } = "";


    /// <summary>
    /// The collection of <see cref="LuceneIndexChannelConfiguration"/> representing the channels and their associated paths to be indexed.
    /// </summary>
    public IEnumerable<LuceneIndexChannelConfiguration> Channels { get; set; } = Enumerable.Empty<LuceneIndexChannelConfiguration>();


    /// <summary>
    /// The collection of <see cref="DataClassInfoBase{DataClassInfoBase}.ClassName"/> names representing the reusable content types associated with the index, derived from <see cref="LuceneReusableContentTypeItemInfo.LuceneReusableContentTypeItemContentTypeName"/>.
    /// </summary>
    public IEnumerable<string> ReusableContentTypeNames { get; set; } = Enumerable.Empty<string>();


    /// <summary>
    /// The parameterless constructor.
    /// </summary>
    public LuceneIndexModel() { }


    /// <summary>
    /// The constructor for creating a Lucene index model from the provided information objects and collections.
    /// </summary>
    /// <param name="index">The name of the index.</param>
    /// <param name="indexLanguages">The languages configured for the index.</param>
    /// <param name="indexPaths">The paths configured for the index.</param>
    /// <param name="contentTypes">The content types configured for the index.</param>
    /// <param name="reusableContentTypes">The reusable content types configured for the index.</param>
    /// <param name="channelInfos">All <see cref="ChannelInfo"/> objects configured in the application.</param>
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
        Channels = indexPaths
            .Where(p => p.LuceneIncludedPathItemIndexItemId == index.LuceneIndexItemId)
            .GroupBy(x => x.LuceneIncludedPathItemChannelName)
            .Select(x => new LuceneIndexChannelConfiguration([.. x], contentTypes, channelInfos))
            .ToList();
    }
}
