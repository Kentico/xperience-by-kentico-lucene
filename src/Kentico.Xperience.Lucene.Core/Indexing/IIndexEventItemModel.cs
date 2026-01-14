using CMS.ContentEngine;

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


    /// <summary>
    /// The <see cref="ContentItemFields.ContentItemGUID"/>
    /// </summary>
    Guid ItemGuid { get; set; }


    /// <summary>
    /// Content language name.
    /// </summary>
    string LanguageName { get; set; }


    /// <summary>
    /// Content item type name.
    /// </summary>
    string ContentTypeName { get; set; }


    /// <summary>
    /// The <see cref="ContentItemFields.ContentItemName"/>
    /// </summary>
    string Name { get; set; }


    /// <summary>
    /// Indicates if content item is secured.
    /// </summary>
    bool IsSecured { get; set; }


    /// <summary>
    /// Content item type identifier.
    /// </summary>
    int ContentTypeID { get; set; }


    /// <summary>
    /// Content language ID.
    /// </summary>
    int ContentLanguageID { get; set; }


    /// <summary>
    /// The representation of the type of this <see cref="IIndexEventItemModel"/>.
    /// </summary>
    IndexEventItemModelType IndexEventItemModelType { get; }


    /// <summary>
    /// Collection of related item references. This is populated during deletion events
    /// to provide information about items that reference the deleted item, enabling
    /// re-indexing of those related items.
    /// </summary>
    IEnumerable<RelatedItemInfo> RelatedItems { get; set; }
}
