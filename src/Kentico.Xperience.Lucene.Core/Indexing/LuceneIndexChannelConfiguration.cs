using System.Text.Json.Serialization;

using CMS.ContentEngine;

namespace Kentico.Xperience.Lucene.Core.Indexing;

/// <summary>
/// The configuration for a specific website channel within a Lucene index, including its display name and the paths to be indexed.
/// </summary>
public sealed class LuceneIndexChannelConfiguration
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


    /// <summary>
    /// The constructor for deserialization purposes only.
    /// </summary>
    /// <param name="websiteChannelName">The <see cref="ChannelInfo.ChannelName"/>.</param>
    /// <param name="channelDisplayName">The <see cref="ChannelInfo.ChannelDisplayName"/>.</param>
    [JsonConstructor]
    public LuceneIndexChannelConfiguration(string websiteChannelName, string channelDisplayName)
    {
        WebsiteChannelName = websiteChannelName;
        ChannelDisplayName = channelDisplayName;
    }


    /// <summary>
    /// The constructor for creating a Lucene index channel configuration. The constructor excpects at least one path to be provided, and it uses the first path to determine the channel name and display name.
    /// </summary>
    /// <param name="paths">A collection of <see cref="LuceneIncludedPathItemInfo"/>. The items must have the same <see cref="LuceneIncludedPathItemInfo.LuceneIncludedPathItemChannelName"/>.</param>
    /// <param name="contentTypes">The configured <see cref="LuceneIndexContentType"/>s.</param>
    /// <param name="channelInfos">The collection expected to contain all <see cref="ChannelInfo"/>s of all the Channels registered in the application.</param>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public LuceneIndexChannelConfiguration(IEnumerable<LuceneIncludedPathItemInfo> paths, IEnumerable<LuceneIndexContentType> contentTypes, IEnumerable<ChannelInfo> channelInfos)
    {
        var representativePath = paths.FirstOrDefault() ??
            throw new InvalidOperationException($"The {nameof(paths)} collection must contain at least one path.");

        ChannelDisplayName = channelInfos.FirstOrDefault(x => x.ChannelName == representativePath.LuceneIncludedPathItemChannelName)?.ChannelDisplayName ??
            throw new ArgumentException($"There must exist a channel for which the {nameof(paths)} are configured.");

        WebsiteChannelName = representativePath.LuceneIncludedPathItemChannelName;

        IncludedPaths = paths.Select(p => new LuceneIndexIncludedPath(p,
            contentTypes.Where(x => x.LucenePathItemId == p.LuceneIncludedPathItemId))
        );
    }
}
