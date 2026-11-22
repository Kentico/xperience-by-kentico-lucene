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
    public int ItemID { get; set; }


    /// <summary>
    /// The <see cref="ContentItemFields.ContentItemGUID"/>
    /// </summary>
    public Guid ItemGuid { get; set; }


    /// <summary>
    /// Content language name.
    /// </summary>
    public string LanguageName { get; set; }


    /// <summary>
    /// Content item type name.
    /// </summary>
    public string ContentTypeName { get; set; }


    /// <summary>
    /// The <see cref="ContentItemFields.ContentItemName"/>
    /// </summary>
    public string Name { get; set; }


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


    /// <summary>
    /// The representation of the type of this <see cref="IIndexEventItemModel"/>.
    /// </summary>
    public IndexEventItemModelType IndexEventItemModelType { get; }
}
