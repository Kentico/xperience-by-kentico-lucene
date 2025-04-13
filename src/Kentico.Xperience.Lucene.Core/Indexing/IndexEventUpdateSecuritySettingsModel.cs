using CMS.Websites;

namespace Kentico.Xperience.Lucene.Core.Indexing;

/// <summary>
/// Represents a security settings modification to a web page.
/// </summary>
public class IndexEventUpdateSecuritySettingsModel
{
    /// <summary>
    /// The <see cref="WebPageFields.WebPageItemID"/> 
    /// </summary>
    public int ItemID { get; set; }
    /// <summary>
    /// The <see cref="WebPageFields.WebPageItemGUID"/>
    /// </summary>
    public Guid ItemGuid { get; set; }
    public string ContentTypeName { get; set; } = string.Empty;
    /// <summary>
    /// The <see cref="WebPageFields.WebPageItemName"/>
    /// </summary>
    public string Name { get; set; } = string.Empty;
    public bool IsSecured { get; set; }
    public int ContentTypeID { get; set; }

    public string WebsiteChannelName { get; set; } = string.Empty;
    public string WebPageItemTreePath { get; set; } = string.Empty;
    public int? ParentID { get; set; }
    public int Order { get; set; }

    public IndexEventUpdateSecuritySettingsModel() { }

    public IndexEventUpdateSecuritySettingsModel(
        int itemID,
        Guid itemGuid,
        string contentTypeName,
        string name,
        bool isSecured,
        int contentTypeID,
        string websiteChannelName,
        string webPageItemTreePath,
        int parentID,
        int order
    )
    {
        ItemID = itemID;
        ItemGuid = itemGuid;
        ContentTypeName = contentTypeName;
        WebsiteChannelName = websiteChannelName;
        WebPageItemTreePath = webPageItemTreePath;
        ParentID = parentID;
        Order = order;
        Name = name;
        IsSecured = isSecured;
        ContentTypeID = contentTypeID;
    }
}
