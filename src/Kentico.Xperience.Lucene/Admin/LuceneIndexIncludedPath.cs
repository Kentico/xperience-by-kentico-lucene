namespace Kentico.Xperience.Lucene.Admin;

public class LuceneIndexIncludedPath
{
    /// <summary>
    /// The node alias pattern that will be used to match pages in the content tree for indexing.
    /// </summary>
    /// <remarks>For example, "/Blogs/Products/" will index all pages under the "Products" page.</remarks>
    public string AliasPath
    {
        get;
    }


    /// <summary>
    /// A list of content types under the specified <see cref="AliasPath"/> that will be indexed.
    /// </summary>
    public string[]? ContentTypes
    {
        get;
        set;
    } = Array.Empty<string>();


    /// <summary>
    /// The internal identifier of the included path.
    /// </summary>
    public string? Identifier
    {
        get;
        set;
    }


    /// <summary>
    /// </summary>
    /// <param name="aliasPath">The node alias pattern that will be used to match pages in the content tree
    /// for indexing.</param>
    public LuceneIndexIncludedPath(string aliasPath) => AliasPath = aliasPath;
}
