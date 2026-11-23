namespace Kentico.Xperience.Lucene.Core.Indexing;

/// <summary>
/// Read-only inspection of Lucene indexes: state, stored documents, query testing and indexing diagnostics.
/// Nothing here modifies an index.
/// </summary>
public interface ILuceneIndexInspector
{
    /// <summary>
    /// Gets the state of the published and pending generations and the last recorded rebuild request.
    /// </summary>
    public LuceneIndexStatus GetStatus(LuceneIndex index);


    /// <summary>
    /// Gets a page of stored documents from the published generation, including all stored fields.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <param name="skip">Number of documents to skip.</param>
    /// <param name="take">Page size, capped at <see cref="LuceneIndexInspectorLimits.MaxPageSize"/>.</param>
    public LuceneStoredDocumentsPage GetDocuments(LuceneIndex index, int skip, int take);


    /// <summary>
    /// Runs a query against the published generation using the index's configured analyzer and returns
    /// hits with their stored fields and Lucene's score explanation.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <param name="queryText">Query in Lucene classic query syntax.</param>
    /// <param name="defaultField">Field to search when the query does not name one.</param>
    /// <param name="take">Maximum hits, capped at <see cref="LuceneIndexInspectorLimits.MaxPageSize"/>.</param>
    public LuceneQueryTestResult TestQuery(LuceneIndex index, string queryText, string defaultField, int take) =>
        TestQuery(index, queryText, defaultField, 0, take);


    /// <summary>
    /// Runs a query like <see cref="TestQuery(LuceneIndex, string, string, int)"/> and returns one page of hits,
    /// so results beyond <see cref="LuceneIndexInspectorLimits.MaxPageSize"/> can be paged through.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <param name="queryText">Query in Lucene classic query syntax.</param>
    /// <param name="defaultField">Field to search when the query does not name one.</param>
    /// <param name="skip">Number of top hits to skip.</param>
    /// <param name="take">Page size, capped at <see cref="LuceneIndexInspectorLimits.MaxPageSize"/>.</param>
    public LuceneQueryTestResult TestQuery(LuceneIndex index, string queryText, string defaultField, int skip, int take);


    /// <summary>
    /// Lists the fields indexed in the published generation. Fields declared by
    /// <see cref="LuceneSearchFieldDescriptor.BaseDocumentFields"/> or the index's
    /// <see cref="ILuceneIndexingStrategy.GetSearchFieldDescriptors"/> have a kind; other indexed fields are untyped.
    /// Stored-only and doc-values-only fields are not listed because they cannot be searched.
    /// </summary>
    /// <param name="index">The index.</param>
    public LuceneSearchFieldsResult GetSearchFields(LuceneIndex index) =>
        new([], "This index inspector does not support typed search fields.");


    /// <summary>
    /// Runs a query in which every filter must match, built from the declared kind of each field, and returns one page of hits.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <param name="filters">Filters on typed fields returned by <see cref="GetSearchFields"/>. No filters match every document.</param>
    /// <param name="skip">Number of top hits to skip.</param>
    /// <param name="take">Page size, capped at <see cref="LuceneIndexInspectorLimits.MaxPageSize"/>.</param>
    public LuceneQueryTestResult FilterQuery(LuceneIndex index, IReadOnlyList<LuceneSearchFilter> filters, int skip, int take) =>
        new(null, 0, [], "This index inspector does not support typed search filters.");


    /// <summary>
    /// Compares the items the index configuration selects with the documents stored in the published generation.
    /// </summary>
    public Task<LuceneIndexDiagnostics> GetDiagnosticsAsync(LuceneIndex index, CancellationToken cancellationToken);


    /// <summary>
    /// Runs diagnostics like <see cref="GetDiagnosticsAsync"/>, keeps the full results for a short time and returns
    /// the counts with the first page of each category. Use <see cref="GetDiagnosticsPage"/> for further pages.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <param name="take">Rows in the first page of each category, capped at <see cref="LuceneIndexInspectorLimits.MaxPageSize"/>.</param>
    /// <param name="cancellationToken">Cancels the run.</param>
    public async Task<LuceneDiagnosticsRun> RunDiagnosticsAsync(LuceneIndex index, int take, CancellationToken cancellationToken) =>
        new(null, await GetDiagnosticsAsync(index, cancellationToken));


    /// <summary>
    /// Gets one page of a category from a run started by <see cref="RunDiagnosticsAsync"/>.
    /// Runs expire after a few minutes or when newer runs replace them.
    /// </summary>
    /// <param name="index">The index the run belongs to.</param>
    /// <param name="runId">The run's <see cref="LuceneDiagnosticsRun.RunId"/>.</param>
    /// <param name="category">The category to page.</param>
    /// <param name="skip">Number of rows to skip.</param>
    /// <param name="take">Page size, capped at <see cref="LuceneIndexInspectorLimits.MaxPageSize"/>.</param>
    public LuceneDiagnosticsPage GetDiagnosticsPage(LuceneIndex index, Guid runId, LuceneDiagnosticCategory category, int skip, int take) =>
        new(0, [], "This index inspector does not support paging diagnostics.");
}


/// <summary>
/// Limits applied by <see cref="ILuceneIndexInspector"/>.
/// </summary>
public static class LuceneIndexInspectorLimits
{
    /// <summary>Largest page or hit count returned per call.</summary>
    public const int MaxPageSize = 50;

    /// <summary>Largest number of diagnostic rows returned per category.</summary>
    public const int MaxDiagnosticRows = 200;

    /// <summary>Largest number of filters accepted by <see cref="ILuceneIndexInspector.FilterQuery"/>.</summary>
    public const int MaxFilters = 20;
}


/// <param name="Name">Lucene field name.</param>
/// <param name="DisplayName">Label from the descriptor, or the field name.</param>
/// <param name="Kind">Declared kind, or <see langword="null"/> when no descriptor declares the field.</param>
public sealed record LuceneSearchField(string Name, string DisplayName, LuceneSearchFieldKind? Kind);


/// <param name="Fields">Indexed fields, declared fields first.</param>
/// <param name="Error">Set when the fields could not be read.</param>
public sealed record LuceneSearchFieldsResult(IReadOnlyList<LuceneSearchField> Fields, string? Error);


/// <param name="Field">Name of a typed field.</param>
/// <param name="Value">Text to analyze, exact value, or <c>true</c>/<c>false</c> for boolean fields.</param>
public sealed record LuceneSearchFilter(string Field, string Value);


/// <param name="Generation">Generation number.</param>
/// <param name="IsPublished">Whether this generation is the one searched.</param>
public sealed record LuceneGenerationInfo(int Generation, bool IsPublished);


/// <param name="HasPublishedGeneration">Whether a generation has been published and can be searched.</param>
/// <param name="PublishedGeneration">The searched generation number, when one exists.</param>
/// <param name="Generations">All generations present in storage.</param>
/// <param name="DocumentCount">Live documents in the published generation.</param>
/// <param name="DeletedDocumentCount">Documents marked deleted but not yet merged away.</param>
/// <param name="LastUpdatedUtc">Last write time of the index directory, when known.</param>
/// <param name="LastRebuildRequest">Last rebuild request on this instance. Does not reflect whether it finished.</param>
/// <param name="ReadError">Set when the published generation could not be read.</param>
public sealed record LuceneIndexStatus(
    bool HasPublishedGeneration,
    int? PublishedGeneration,
    IReadOnlyList<LuceneGenerationInfo> Generations,
    int DocumentCount,
    int DeletedDocumentCount,
    DateTime? LastUpdatedUtc,
    LuceneRebuildRecord? LastRebuildRequest,
    string? ReadError);


/// <param name="Name">Field name.</param>
/// <param name="Value">Stored string value.</param>
public sealed record LuceneStoredField(string Name, string Value);


/// <param name="DocumentId">Lucene internal document number (not stable across merges).</param>
/// <param name="Fields">Stored fields.</param>
public sealed record LuceneStoredDocument(int DocumentId, IReadOnlyList<LuceneStoredField> Fields);


/// <param name="Total">Live documents in the generation.</param>
/// <param name="Documents">The requested page.</param>
/// <param name="ReadError">Set when the published generation could not be read.</param>
public sealed record LuceneStoredDocumentsPage(int Total, IReadOnlyList<LuceneStoredDocument> Documents, string? ReadError);


/// <param name="Document">The stored document.</param>
/// <param name="Score">Lucene score.</param>
/// <param name="Explanation">Lucene score explanation.</param>
public sealed record LuceneQueryHit(LuceneStoredDocument Document, float Score, string Explanation);


/// <param name="ParsedQuery">The query after parsing with the index analyzer.</param>
/// <param name="TotalHits">Total matching documents.</param>
/// <param name="Hits">The requested page of hits, highest score first.</param>
/// <param name="Error">Set for invalid queries or unreadable indexes.</param>
public sealed record LuceneQueryTestResult(string? ParsedQuery, int TotalHits, IReadOnlyList<LuceneQueryHit> Hits, string? Error);


/// <param name="ItemGuid">Content item GUID.</param>
/// <param name="LanguageName">Language.</param>
/// <param name="ContentTypeName">Content type.</param>
/// <param name="Name">Item name.</param>
/// <param name="Reason">Why the item is in this category.</param>
public sealed record LuceneDiagnosticItem(Guid ItemGuid, string LanguageName, string ContentTypeName, string Name, string Reason);


/// <param name="SelectedItemCount">Distinct items (GUID and language) selected by the configuration.</param>
/// <param name="IndexableCount">Selected items the strategy maps to a document.</param>
/// <param name="SkippedCount">Selected items the strategy skips.</param>
/// <param name="MissingCount">Indexable items with no stored document.</param>
/// <param name="OrphanCount">Stored documents that are not selected by the configuration.</param>
/// <param name="Skipped">Skipped items (capped).</param>
/// <param name="Missing">Indexable items not in the index (capped).</param>
/// <param name="Orphans">Stored documents not selected (capped).</param>
/// <param name="Error">Set when diagnostics could not run.</param>
public sealed record LuceneIndexDiagnostics(
    int SelectedItemCount,
    int IndexableCount,
    int SkippedCount,
    int MissingCount,
    int OrphanCount,
    IReadOnlyList<LuceneDiagnosticItem> Skipped,
    IReadOnlyList<LuceneDiagnosticItem> Missing,
    IReadOnlyList<LuceneDiagnosticItem> Orphans,
    string? Error);


/// <summary>A category of diagnostic rows.</summary>
public enum LuceneDiagnosticCategory
{
    /// <summary>Selected items the strategy skips.</summary>
    Skipped,

    /// <summary>Indexable items with no stored document.</summary>
    Missing,

    /// <summary>Stored documents that are not selected by the configuration.</summary>
    Orphans,
}


/// <param name="RunId">Identifies the kept results for <see cref="ILuceneIndexInspector.GetDiagnosticsPage"/>; <see langword="null"/> when they cannot be paged.</param>
/// <param name="Diagnostics">Counts, with the first page of each category.</param>
public sealed record LuceneDiagnosticsRun(Guid? RunId, LuceneIndexDiagnostics Diagnostics);


/// <param name="Total">Rows in the category.</param>
/// <param name="Items">The requested page.</param>
/// <param name="Error">Set when the run expired or was not found.</param>
public sealed record LuceneDiagnosticsPage(int Total, IReadOnlyList<LuceneDiagnosticItem> Items, string? Error);
