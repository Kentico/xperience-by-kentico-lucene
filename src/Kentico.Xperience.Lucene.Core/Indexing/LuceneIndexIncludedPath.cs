using System.Text.Json.Serialization;

namespace Kentico.Xperience.Lucene.Core.Indexing;

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
    public List<LuceneIndexContentType> ContentTypes { get; set; } = [];

    /// <summary>
    /// The internal identifier of the included path.
    /// </summary>
    public int? Identifier { get; set; }

    [JsonConstructor]
    public LuceneIndexIncludedPath(string aliasPath) => AliasPath = aliasPath;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="indexPath"></param>
    /// <param name="contentTypes"></param>
    public LuceneIndexIncludedPath(LuceneIncludedPathItemInfo indexPath, IEnumerable<LuceneIndexContentType> contentTypes)
    {
        AliasPath = indexPath.LuceneIncludedPathItemAliasPath;
        ContentTypes = contentTypes.ToList();
        Identifier = indexPath.LuceneIncludedPathItemId;
    }
}
