using System.Text.Json.Serialization;

using CMS.ContentEngine;

namespace Kentico.Xperience.Lucene.Core.Indexing;

/// <summary>
/// The configuration for a specific website channel within a Lucene index, including its display name and the paths to be indexed.
/// </summary>
public class LuceneIndexChannelConfiguration
{
    /// <summary>
    /// The code name of the website channel associated with this configuration.
    /// </summary>
    public string WebsiteChannelName { get; set; }


    /// <summary>
    /// The display name of the website channel.
    /// </summary>
    public string ChannelDisplayName { get; set; }


    /// <summary>
    /// The collection of paths to be indexed for this channel.
    /// </summary>
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
