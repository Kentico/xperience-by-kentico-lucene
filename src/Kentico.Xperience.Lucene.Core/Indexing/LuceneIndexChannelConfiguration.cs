using System.Text.Json.Serialization;

using CMS.ContentEngine;

namespace Kentico.Xperience.Lucene.Core.Indexing;
public class LuceneIndexChannelConfiguration
{
    public string WebsiteChannelName { get; set; }
    public string ChannelDisplayName { get; set; }
    public IEnumerable<LuceneIndexIncludedPath> IncludedPaths { get; set; } = [];

    [JsonConstructor]
    public LuceneIndexChannelConfiguration(string websiteChannelName, string channelDisplayName)
    {
        WebsiteChannelName = websiteChannelName;
        ChannelDisplayName = channelDisplayName;
    }

    public LuceneIndexChannelConfiguration(IEnumerable<LuceneIncludedPathItemInfo> paths, IEnumerable<LuceneIndexContentType> contentTypes, IEnumerable<ChannelInfo> channelInfos)
    {
        var representant = paths.First();

        ChannelDisplayName = channelInfos.First(x => x.ChannelName == representant.LuceneIncludedPathItemChannelName).ChannelDisplayName;
        WebsiteChannelName = representant.LuceneIncludedPathItemChannelName;
        IncludedPaths = paths.Select(p => new LuceneIndexIncludedPath(p,
            contentTypes.Where(x => x.LucenePathItemId == p.LuceneIncludedPathItemId))
        );
    }
}
