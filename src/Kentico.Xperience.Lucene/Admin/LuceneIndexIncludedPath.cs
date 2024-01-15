namespace Kentico.Xperience.Lucene.Admin;

public class LuceneIndexIncludedPath
{
    /// <summary>
    /// The node alias pattern that will be used to match pages in the content tree for indexing.
    /// </summary>
    /// <remarks>For example, "/Blogs/Products/" will index all pages under the "Products" page.</remarks>
    public string AliasPath { get; }

    /// <summary>
    /// A list of content types under the specified <see cref="AliasPath"/> that will be indexed.
    /// </summary>
    public List<string> ContentTypes { get; set; } = new();

    /// <summary>
    /// The internal identifier of the included path.
    /// </summary>
    public string? Identifier { get; set; }

    public LuceneIndexIncludedPath(string aliasPath) => AliasPath = aliasPath;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="indexPath"></param>
    /// <param name="contentTypes"></param>
    public LuceneIndexIncludedPath(LuceneIncludedPathItemInfo indexPath, IEnumerable<LuceneContentTypeItemInfo> contentTypes)
    {
        AliasPath = indexPath.LuceneIncludedPathItemAliasPath;
        ContentTypes = contentTypes.Where(y => indexPath.LuceneIncludedPathItemId == y.LuceneContentTypeItemIncludedPathItemId).Select(y => y.LuceneContentTypeItemContentTypeName).ToList();
        Identifier = indexPath.LuceneIncludedPathItemId.ToString();
    }
}
