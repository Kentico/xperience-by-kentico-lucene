namespace Kentico.Xperience.Lucene.Core.Indexing;

/// <summary>
/// Represents information about an item that references another item.
/// Used during deletion events to track which items need to be re-indexed.
/// </summary>
public class RelatedItemInfo
{
    /// <summary>
    /// The identifier of the related item.
    /// </summary>
    public int ItemID { get; set; }


    /// <summary>
    /// The GUID of the related item.
    /// </summary>
    public Guid ItemGuid { get; set; }


    /// <summary>
    /// Content type name of the related item.
    /// </summary>
    public string ContentTypeName { get; set; } = string.Empty;


    /// <summary>
    /// Content language name of the related item.
    /// </summary>
    public string LanguageName { get; set; } = string.Empty;


    public RelatedItemInfo() { }


    public RelatedItemInfo(int itemID, Guid itemGuid, string contentTypeName, string languageName)
    {
        ItemID = itemID;
        ItemGuid = itemGuid;
        ContentTypeName = contentTypeName;
        LanguageName = languageName;
    }
}
