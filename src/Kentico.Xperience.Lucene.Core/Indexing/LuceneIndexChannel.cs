namespace Kentico.Xperience.Lucene.Core.Indexing;

/// <summary>
/// The configuration for a specific website channel associated with a Lucene index.
/// </summary>
public class LuceneIndexChannel
{
    /// <summary>
    /// The code name of the website channel.
    /// </summary>
    public string ChannelName { get; set; } = string.Empty;


    /// <summary>
    /// The display name of the website channel.
    /// </summary>
    public string ChannelDisplayName { get; set; } = string.Empty;


    /// <summary>
    /// The default constructor.
    /// </summary>
    public LuceneIndexChannel()
    { }


    public LuceneIndexChannel(string channelName, string channelDisplayName)
    {
        ChannelName = channelName;
        ChannelDisplayName = channelDisplayName;
    }
}
