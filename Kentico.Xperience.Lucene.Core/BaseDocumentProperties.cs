using Kentico.Xperience.Lucene.Core.Indexing;

using Lucene.Net.Documents;

namespace Kentico.Xperience.Lucene.Core;

/// <summary>
/// Properties automatically added to each indexed item
/// </summary>
public static class BaseDocumentProperties
{
    public const string ID = "ID";
    public const string CONTENT_TYPE_NAME = "ContentTypeName";
    public const string ITEM_GUID = "ItemGuid";
    public const string LANGUAGE_NAME = "LanguageName";
    /// <summary>
    /// By default this field on the <see cref="Document"/> is populated with a web page's relative path
    /// if the indexed item is a web page. The field is not added to a document for reusable content items.
    /// 
    /// If a field with this name has already been added to the document by
    /// custom <see cref="ILuceneIndexingStrategy"/> it will not be overridden.
    /// This enables a developer to choose if they want to use relative or absolute URLs
    /// </summary>
    public const string URL = "Url";
}
