namespace Kentico.Xperience.Lucene.Core.Indexing;

/// <summary>
/// Lucene search integration options.
/// </summary>
public sealed class LuceneSearchOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string CMS_LUCENE_SEARCH_SECTION_NAME = "CMSLuceneSearch";


    /// <summary>
    /// If true, the items that require authentication will be included in the indexing process.
    /// </summary>
    public bool IncludeSecuredItems { get; set; } = false;


    /// <summary>
    /// Modifies the options for reindexing operations performed after application startup.
    /// </summary>
    public AutomaticReindexingOptions? PostStartupReindexingOptions { get; set; }
}
