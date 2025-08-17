namespace Kentico.Xperience.Lucene.Core.Indexing;

public class LuceneIndexChannel
{
    public string ChannelName { get; set; } = string.Empty;
    public string ChannelDisplayName { get; set; } = string.Empty;

    public LuceneIndexChannel()
    { }

    public LuceneIndexChannel(string channelName, string channelDisplayName)
    {
        ChannelName = channelName;
        ChannelDisplayName = channelDisplayName;
    }
}
