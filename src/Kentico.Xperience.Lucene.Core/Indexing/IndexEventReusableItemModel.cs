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


    /// <summary>
    /// Content language name.
    /// </summary>
    public string LanguageName { get; set; } = string.Empty;


    /// <summary>
    /// Content item type name.
    /// </summary>
    public string ContentTypeName { get; set; } = string.Empty;


    /// <summary>
    /// The <see cref="ContentItemFields.ContentItemName"/>
    /// </summary>
    public string Name { get; set; } = string.Empty;


    /// <summary>
    /// Indicates if content item is secured.
    /// </summary>
    public bool IsSecured { get; set; }


    /// <summary>
    /// Content item type identifier.
    /// </summary>
    public int ContentTypeID { get; set; }


    /// <summary>
    /// Content language ID.
    /// </summary>
    public int ContentLanguageID { get; set; }


    /// <inheritdoc/>
    public IndexEventItemModelType IndexEventItemModelType { get; } = IndexEventItemModelType.ReusableItem;


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
