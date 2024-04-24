namespace Kentico.Xperience.Lucene.Core.Indexing;

public class LuceneIndexContentType
{
    /// <summary>
    /// Name of the indexed content type for an indexed path
    /// </summary>
    public string ContentTypeName { get; set; } = "";

    /// <summary>
    /// Displayed name of the indexed content type for an indexed path which will be shown in admin UI
    /// </summary>
    public string ContentTypeDisplayName { get; set; } = "";

    /// <summary>
    /// Id of the associated Lucene path item
    /// </summary>
    public int LucenePathItemId { get; set; }

    public LuceneIndexContentType()
    { }

    public LuceneIndexContentType(string className, string classDisplayName, int lucenePathItemId)
    {
        ContentTypeName = className;
        ContentTypeDisplayName = classDisplayName;
        LucenePathItemId = lucenePathItemId;
    }
}
