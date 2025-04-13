using CMS.Websites;

namespace Kentico.Xperience.Lucene.Core.Indexing;
/// <summary>
/// Represents a modification to a web page.
/// </summary>
public class IndexEventWebPageItemModel : IIndexEventItemModel
{
    /// <summary>
    /// The <see cref="WebPageFields.WebPageItemID"/> 
    /// </summary>
    public int ItemID { get; set; }
    /// <summary>
    /// The <see cref="WebPageFields.WebPageItemGUID"/>
    /// </summary>
    public Guid ItemGuid { get; set; }
    public string LanguageName { get; set; } = string.Empty;
    public string ContentTypeName { get; set; } = string.Empty;
    /// <summary>
    /// The <see cref="WebPageFields.WebPageItemName"/>
    /// </summary>
    public string Name { get; set; } = string.Empty;
    public bool IsSecured { get; set; }
    public int ContentTypeID { get; set; }
    public int ContentLanguageID { get; set; }

    public string WebsiteChannelName { get; set; } = string.Empty;
    public string WebPageItemTreePath { get; set; } = string.Empty;
    public int? ParentID { get; set; }
    public int Order { get; set; }
    public IndexEventWebPageItemModel() { }
    public IndexEventWebPageItemModel(
        int itemID,
        Guid itemGuid,
        string languageName,
        string contentTypeName,
        string name,
        bool isSecured,
        int contentTypeID,
        int contentLanguageID,
        string websiteChannelName,
        string webPageItemTreePath,
        int order,
        int? parentID = null
    )
    {
        ItemID = itemID;
        ItemGuid = itemGuid;
        LanguageName = languageName;
        ContentTypeName = contentTypeName;
        WebsiteChannelName = websiteChannelName;
        WebPageItemTreePath = webPageItemTreePath;
        ParentID = parentID;
        Order = order;
        Name = name;
        IsSecured = isSecured;
        ContentTypeID = contentTypeID;
        ContentLanguageID = contentLanguageID;
    }
}