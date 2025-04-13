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
