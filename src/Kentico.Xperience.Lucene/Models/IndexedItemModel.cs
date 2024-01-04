namespace Kentico.Xperience.Lucene.Models;

public class IndexedItemModel
{
    public string? LanguageCode { get; set; }
    public string? ClassName { get; set; }
    public string? ChannelName { get; set; }
    public Guid? WebPageItemGuid { get; set; }
    public string? WebPageItemTreePath { get; set; }
}

public class IndexedContentItemModel
{
    public string? LanguageCode { get; set; }
    public string? ClassName { get; set; }
    public Guid ContentItemGuid { get; set; }
}
