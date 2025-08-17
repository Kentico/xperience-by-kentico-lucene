using System.Text.Json.Serialization;
using CMS.ContentEngine;

namespace Kentico.Xperience.Lucene.Core.Indexing;
public class LuceneIndexChannelConfiguration
{
    public string ChannelName { get; set; }
    public string ChannelDisplayName { get; set; }
    public IEnumerable<LuceneIndexIncludedPath> IncludedPaths { get; set; } = [];

    [JsonConstructor]
    public LuceneIndexChannelConfiguration(string channelName, string channelDisplayName)
    {
        ChannelName = channelName;
        ChannelDisplayName = channelDisplayName;
    }

    public LuceneIndexChannelConfiguration(IEnumerable<LuceneIncludedPathItemInfo> paths, IEnumerable<LuceneIndexContentType> contentTypes, IEnumerable<ChannelInfo> channelInfos)
    {
        var representant = paths.First();

        ChannelDisplayName = channelInfos.First(x => x.ChannelName == representant.LuceneIncludedPathItemChannelName).ChannelDisplayName;
        ChannelName = representant.LuceneIncludedPathItemChannelName;
        IncludedPaths = paths.Select(p => new LuceneIndexIncludedPath(p,
            contentTypes.Where(x => x.LucenePathItemId == p.LuceneIncludedPathItemId))
        );
    }
}
