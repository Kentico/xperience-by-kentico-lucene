namespace Kentico.Xperience.Lucene.Models;

public class IndexedItemModel
{
    public string LanguageName { get; set; }
    public string ContentTypeName { get; set; }
    public string ChannelName { get; set; }
    public Guid WebPageItemGuid { get; set; }
    public string WebPageItemTreePath { get; set; }

    public int? ID { get; set; }
    public int? ParentID { get; set; }
    public string? Name { get; set; }
    public int? Order { get; set; }
    public string? DisplayName { get; set; }
    public bool? IsSecured { get; set; }
    public int? WebsiteChannelID { get; set; }
    public int? ContentTypeID { get; set; }
    public int? ContentLanguageID { get; set; }

    public IndexedItemModel(string languageName,
        string contentTypeName,
        string channelName,
        Guid webPageItemGuid,
        string webPageItemTreePath)
    {
        LanguageName = languageName;
        ContentTypeName = contentTypeName;
        ChannelName = channelName;
        WebPageItemGuid = webPageItemGuid;
        WebPageItemTreePath = webPageItemTreePath;
    }
}

public class IndexedContentItemModel
{
    public string LanguageName { get; set; }
    public string ContentTypeName { get; set; }
    public int ContentItemID { get; set; }
    public Guid ContentItemGuid { get; set; }

    public string? Name { get; set; }
    public string? DisplayName { get; set; }
    public bool? IsSecured { get; set; }
    public int ContentTypeID { get; init; }
    public int? ContentLanguageID { get; set; }

    public IndexedContentItemModel(string languageName, string contentTypeName, int contentItemID, Guid contentItemGuid)
    {
        LanguageName = languageName;
        ContentTypeName = contentTypeName;
        ContentItemID = contentItemID;
        ContentItemGuid = contentItemGuid;
    }
}
