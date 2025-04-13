using CMS.ContentEngine;

namespace Kentico.Xperience.Lucene.Core.Indexing;
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
