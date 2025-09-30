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
    public string WebsiteChannelName { get; init; } = string.Empty;


    /// <summary>
    /// The display name of the website channel.
    /// </summary>
    public string ChannelDisplayName { get; init; } = string.Empty;


    /// <summary>
    /// The collection of paths to be indexed for this channel.
    /// </summary>
    public IEnumerable<LuceneIndexIncludedPath> IncludedPaths { get; init; } = [];


    /// <summary>
    /// The constructor for deserialization purposes only.
    /// </summary>
    /// <param name="websiteChannelName">The <see cref="ChannelInfo.ChannelName"/>.</param>
    /// <param name="channelDisplayName">The <see cref="ChannelInfo.ChannelDisplayName"/>.</param>
    [JsonConstructor]
    internal LuceneIndexChannelConfiguration(string websiteChannelName, string channelDisplayName)
    {
        WebsiteChannelName = websiteChannelName;
        ChannelDisplayName = channelDisplayName;
    }


    /// <summary>
    /// The constructor for creating a Lucene index channel configuration. The constructor expects at least one path to be provided, and it uses the first path to determine the channel name and display name.
    /// </summary>
    /// <param name="paths">A collection of <see cref="LuceneIncludedPathItemInfo"/>. The items must have the same <see cref="LuceneIncludedPathItemInfo.LuceneIncludedPathItemChannelName"/>.</param>
    /// <param name="contentTypes">The configured <see cref="LuceneIndexContentType"/>s.</param>
    /// <param name="channels">The collection expected to contain all <see cref="ChannelInfo"/>s of all the Channels registered in the application.</param>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public LuceneIndexChannelConfiguration(IEnumerable<LuceneIncludedPathItemInfo> paths, IEnumerable<LuceneIndexContentType> contentTypes, IEnumerable<ChannelInfo> channels)
    {
        var representativePath = paths.FirstOrDefault() ??
            throw new InvalidOperationException($"The {nameof(paths)} collection must contain at least one path.");

        // Handle case where channel name is missing (for migration scenarios)
        var channelName = representativePath.LuceneIncludedPathItemChannelName;
        if (string.IsNullOrEmpty(channelName))
        {
            // Try to use the first available channel as a fallback
            var firstChannel = channels.FirstOrDefault();
            if (firstChannel != null)
            {
                channelName = firstChannel.ChannelName;
                ChannelDisplayName = firstChannel.ChannelDisplayName;
            }
            else
            {
                // If no channels are available, create a placeholder
                channelName = "DefaultChannel";
                ChannelDisplayName = "Default Channel";
            }
        }
        else
        {
            var foundChannel = channels.FirstOrDefault(x => string.Equals(x.ChannelName, channelName, StringComparison.InvariantCultureIgnoreCase));
            ChannelDisplayName = foundChannel?.ChannelDisplayName ?? $"{channelName} (Channel Not Found)";
        }

        WebsiteChannelName = channelName ?? string.Empty;

        IncludedPaths = paths.Select(p => new LuceneIndexIncludedPath(p,
            contentTypes.Where(x => x.LucenePathItemId == p.LuceneIncludedPathItemId))
        );
    }
}
