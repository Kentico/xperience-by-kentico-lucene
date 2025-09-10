namespace Kentico.Xperience.Lucene.Core.Indexing;

/// <summary>
/// The configuration for a specific website channel associated with a Lucene index.
/// </summary>
public sealed class LuceneIndexChannel
{
    /// <summary>
    /// The code name of the website channel.
    /// </summary>
    public string ChannelName { get; init; } = string.Empty;


    /// <summary>
    /// The display name of the website channel.
    /// </summary>
    public string ChannelDisplayName { get; init; } = string.Empty;


    /// <summary>
    /// The default constructor.
    /// </summary>
    public LuceneIndexChannel()
    { }

    /// <summary>
    /// The constructor initializing all properties.
    /// </summary>
    /// <param name="channelName">The <see cref="CMS.ContentEngine.ChannelInfo.ChannelName"/>.</param>
    /// <param name="channelDisplayName">The <see cref="CMS.ContentEngine.ChannelInfo.ChannelDisplayName"/>.</param>
    public LuceneIndexChannel(string channelName, string channelDisplayName)
    {
        ChannelName = channelName;
        ChannelDisplayName = channelDisplayName;
    }
}
