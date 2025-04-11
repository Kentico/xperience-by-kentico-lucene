using CMS.ContentEngine;
using CMS.Websites;

namespace Kentico.Xperience.Lucene.Core.Indexing;

/// <summary>
/// Abstraction of different types of events generated from content modifications
/// </summary>
public interface IIndexEventItemModel
{
    /// <summary>
    /// The identifier of the item
    /// </summary>
    int ItemID { get; set; }
    Guid ItemGuid { get; set; }
    string LanguageName { get; set; }
    string ContentTypeName { get; set; }
    string Name { get; set; }
    bool IsSecured { get; set; }
    int ContentTypeID { get; set; }
    int ContentLanguageID { get; set; }
}

/// <summary>
/// Represents a modification to a web page
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
        int parentID,
        int order
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
        int order
    )
    {
        ItemID = itemID;
        ItemGuid = itemGuid;
        LanguageName = languageName;
        ContentTypeName = contentTypeName;
        WebsiteChannelName = websiteChannelName;
        WebPageItemTreePath = webPageItemTreePath;
        Order = order;
        Name = name;
        IsSecured = isSecured;
        ContentTypeID = contentTypeID;
        ContentLanguageID = contentLanguageID;
    }
}

/// <summary>
/// Represents a modification to a reusable content item
/// </summary>
public class IndexEventReusableItemModel : IIndexEventItemModel
{
    /// <summary>
    /// The <see cref="ContentItemFields.ContentItemID"/>
    /// </summary>
    public int ItemID { get; set; }
    /// <summary>
    /// The <see cref="ContentItemFields.ContentItemGUID"/>
    /// </summary>
    public Guid ItemGuid { get; set; }
    public string LanguageName { get; set; } = string.Empty;
    public string ContentTypeName { get; set; } = string.Empty;
    /// <summary>
    /// The <see cref="ContentItemFields.ContentItemName"/>
    /// </summary>
    public string Name { get; set; } = string.Empty;
    public bool IsSecured { get; set; }
    public int ContentTypeID { get; set; }
    public int ContentLanguageID { get; set; }
    public IndexEventReusableItemModel() { }
    public IndexEventReusableItemModel(
        int itemID,
        Guid itemGuid,
        string languageName,
        string contentTypeName,
        string name,
        bool isSecured,
        int contentTypeID,
        int contentLanguageID
    )
    {
        ItemID = itemID;
        ItemGuid = itemGuid;
        LanguageName = languageName;
        ContentTypeName = contentTypeName;
        Name = name;
        IsSecured = isSecured;
        ContentTypeID = contentTypeID;
        ContentLanguageID = contentLanguageID;
    }
}
