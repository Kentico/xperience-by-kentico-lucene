namespace Kentico.Xperience.Lucene.Core.Indexing;

/// <summary>
/// How an indexed field is matched by typed search filters.
/// </summary>
public enum LuceneSearchFieldKind
{
    /// <summary>Tokenized text field, such as a <c>TextField</c>. Plain filter values are analyzed and all terms must match; wildcard terms are lowercased but not analyzed.</summary>
    Text,

    /// <summary>Untokenized field, such as a <c>StringField</c>. Plain values match exactly; wildcard patterns match indexed terms. Case is preserved.</summary>
    Exact,

    /// <summary>Untokenized field holding <see cref="LuceneSearchFieldDescriptor.TrueValue"/> or <see cref="LuceneSearchFieldDescriptor.FalseValue"/>.</summary>
    Boolean
}


/// <summary>
/// Declares how an indexed field can be searched. Indexing strategies return these from
/// <see cref="ILuceneIndexingStrategy.GetSearchFieldDescriptors"/> so tools such as the administration
/// Search tab can offer typed filters. Declare only fields the strategy actually indexes.
/// </summary>
/// <param name="Name">Lucene field name.</param>
/// <param name="Kind">How values are matched.</param>
/// <param name="DisplayName">Label shown to editors. Defaults to <paramref name="Name"/>.</param>
/// <param name="TrueValue">Indexed value meaning true, for <see cref="LuceneSearchFieldKind.Boolean"/> fields.</param>
/// <param name="FalseValue">Indexed value meaning false, for <see cref="LuceneSearchFieldKind.Boolean"/> fields.</param>
public sealed record LuceneSearchFieldDescriptor(
    string Name,
    LuceneSearchFieldKind Kind,
    string? DisplayName = null,
    string TrueValue = "true",
    string FalseValue = "false")
{
    /// <summary>
    /// Descriptors for the <see cref="BaseDocumentProperties"/> fields added to every document.
    /// </summary>
    public static IReadOnlyList<LuceneSearchFieldDescriptor> BaseDocumentFields { get; } =
    [
        new(BaseDocumentProperties.CONTENT_TYPE_NAME, LuceneSearchFieldKind.Exact, "Content type code name"),
        new(BaseDocumentProperties.LANGUAGE_NAME, LuceneSearchFieldKind.Exact, "Language"),
        new(BaseDocumentProperties.ITEM_GUID, LuceneSearchFieldKind.Exact, "Item GUID"),
        new(BaseDocumentProperties.URL, LuceneSearchFieldKind.Exact, "URL"),
    ];
}
