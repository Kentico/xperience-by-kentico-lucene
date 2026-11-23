using Kentico.Xperience.Lucene.Core.Search;

using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Util;

using Microsoft.Extensions.DependencyInjection;

namespace Kentico.Xperience.Lucene.Core.Indexing;

internal class DefaultLuceneIndexInspector : ILuceneIndexInspector
{
    private readonly ILuceneSearchService searchService;
    private readonly LuceneIndexedItemEnumerator itemEnumerator;
    private readonly LuceneRebuildTracker rebuildTracker;
    private readonly IServiceProvider serviceProvider;
    private readonly LuceneDiagnosticsSnapshotStore diagnosticsSnapshots = new();


    public DefaultLuceneIndexInspector(
        ILuceneSearchService searchService,
        LuceneIndexedItemEnumerator itemEnumerator,
        LuceneRebuildTracker rebuildTracker,
        IServiceProvider serviceProvider)
    {
        this.searchService = searchService;
        this.itemEnumerator = itemEnumerator;
        this.rebuildTracker = rebuildTracker;
        this.serviceProvider = serviceProvider;
    }


    public LuceneIndexStatus GetStatus(LuceneIndex index)
    {
        var generations = index.StorageContext.GetExistingGenerations();
        var published = generations.Where(g => g.IsPublished).MaxBy(g => g.Generation);
        var infos = generations.Select(g => new LuceneGenerationInfo(g.Generation, g == published)).ToList();
        var lastRebuild = rebuildTracker.GetLastRequest(index.IndexName);

        if (published is null)
        {
            return new LuceneIndexStatus(false, null, infos, 0, 0, null, lastRebuild, null);
        }

        try
        {
            var (count, deleted) = searchService.UseSearcher(index, searcher =>
                (searcher.IndexReader.NumDocs, searcher.IndexReader.NumDeletedDocs));

            return new LuceneIndexStatus(true, published.Generation, infos, count, deleted, GetLastWriteTimeUtc(published.Path), lastRebuild, null);
        }
        catch (Exception ex)
        {
            return new LuceneIndexStatus(true, published.Generation, infos, 0, 0, null, lastRebuild, ex.Message);
        }
    }


    public LuceneStoredDocumentsPage GetDocuments(LuceneIndex index, int skip, int take)
    {
        if (index.StorageContext.TryGetPublishedIndex() is null)
        {
            return new LuceneStoredDocumentsPage(0, [], null);
        }

        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, LuceneIndexInspectorLimits.MaxPageSize);

        try
        {
            return searchService.UseSearcher(index, searcher =>
            {
                var top = searcher.Search(new MatchAllDocsQuery(), skip + take);
                var documents = top.ScoreDocs
                    .Skip(skip)
                    .Select(hit => ToStoredDocument(searcher, hit.Doc))
                    .ToList();

                return new LuceneStoredDocumentsPage(top.TotalHits, documents, null);
            });
        }
        catch (Exception ex)
        {
            return new LuceneStoredDocumentsPage(0, [], ex.Message);
        }
    }


    public LuceneQueryTestResult TestQuery(LuceneIndex index, string queryText, string defaultField, int skip, int take)
    {
        if (string.IsNullOrWhiteSpace(queryText))
        {
            return new LuceneQueryTestResult(null, 0, [], "Enter a query.");
        }

        if (index.StorageContext.TryGetPublishedIndex() is null)
        {
            return new LuceneQueryTestResult(null, 0, [], "The index has no published generation to search.");
        }

        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, LuceneIndexInspectorLimits.MaxPageSize);
        string field = string.IsNullOrWhiteSpace(defaultField) ? nameof(IIndexEventItemModel.Name) : defaultField.Trim();

        Query query;
        try
        {
            var parser = new QueryParser(AnalyzerStorage.AnalyzerLuceneVersion, field, index.LuceneAnalyzer);
            query = parser.Parse(queryText);
        }
        catch (ParseException ex)
        {
            return new LuceneQueryTestResult(null, 0, [], $"Invalid query: {ex.Message}");
        }

        try
        {
            return searchService.UseSearcher(index, searcher => Search(searcher, query, skip, take));
        }
        catch (Exception ex)
        {
            return new LuceneQueryTestResult(query.ToString(), 0, [], ex.Message);
        }
    }


    public LuceneSearchFieldsResult GetSearchFields(LuceneIndex index)
    {
        if (index.StorageContext.TryGetPublishedIndex() is null)
        {
            return new LuceneSearchFieldsResult([], null);
        }

        try
        {
            var descriptors = GetDescriptors(index);
            var fields = searchService.UseSearcher(index, searcher => GetSearchFields(searcher, descriptors));

            return new LuceneSearchFieldsResult(fields, null);
        }
        catch (Exception ex)
        {
            return new LuceneSearchFieldsResult([], ex.Message);
        }
    }


    public LuceneQueryTestResult FilterQuery(LuceneIndex index, IReadOnlyList<LuceneSearchFilter> filters, int skip, int take)
    {
        if (filters.Count > LuceneIndexInspectorLimits.MaxFilters)
        {
            return new LuceneQueryTestResult(null, 0, [], $"Use at most {LuceneIndexInspectorLimits.MaxFilters} filters.");
        }

        if (index.StorageContext.TryGetPublishedIndex() is null)
        {
            return new LuceneQueryTestResult(null, 0, [], "The index has no published generation to search.");
        }

        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, LuceneIndexInspectorLimits.MaxPageSize);

        IReadOnlyDictionary<string, LuceneSearchFieldDescriptor> descriptors;
        try
        {
            descriptors = GetDescriptors(index);
        }
        catch (Exception ex)
        {
            return new LuceneQueryTestResult(null, 0, [], ex.Message);
        }

        Query? query = null;
        try
        {
            return searchService.UseSearcher(index, searcher =>
            {
                var typed = GetSearchFields(searcher, descriptors)
                    .Where(f => f.Kind is not null)
                    .Select(f => f.Name)
                    .ToHashSet(StringComparer.Ordinal);

                var boolean = new BooleanQuery();
                foreach (var filter in filters)
                {
                    string field = filter.Field ?? "";
                    if (!typed.Contains(field) || !descriptors.TryGetValue(field, out var descriptor))
                    {
                        return new LuceneQueryTestResult(null, 0, [], $"{field} is not a typed field indexed in the published generation.");
                    }

                    var (clause, error) = BuildFilterQuery(index, descriptor, filter.Value ?? "");
                    if (clause is null)
                    {
                        return new LuceneQueryTestResult(null, 0, [], error);
                    }

                    boolean.Add(clause, Occur.MUST);
                }

                query = boolean.Clauses.Count == 0 ? new MatchAllDocsQuery() : boolean;

                return Search(searcher, query, skip, take);
            });
        }
        catch (Exception ex)
        {
            return new LuceneQueryTestResult(query?.ToString(), 0, [], ex.Message);
        }
    }


    public async Task<LuceneIndexDiagnostics> GetDiagnosticsAsync(LuceneIndex index, CancellationToken cancellationToken)
    {
        try
        {
            var result = await ClassifyAsync(index, cancellationToken);

            return result.ToDiagnostics(LuceneIndexInspectorLimits.MaxDiagnosticRows);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new LuceneIndexDiagnostics(0, 0, 0, 0, 0, [], [], [], ex.Message);
        }
    }


    public async Task<LuceneDiagnosticsRun> RunDiagnosticsAsync(LuceneIndex index, int take, CancellationToken cancellationToken)
    {
        take = Math.Clamp(take, 1, LuceneIndexInspectorLimits.MaxPageSize);

        try
        {
            var result = await ClassifyAsync(index, cancellationToken);
            var runId = diagnosticsSnapshots.Add(index.IndexName, result.Skipped, result.Missing, result.Orphans);

            return new LuceneDiagnosticsRun(runId, result.ToDiagnostics(take));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new LuceneDiagnosticsRun(null, new LuceneIndexDiagnostics(0, 0, 0, 0, 0, [], [], [], ex.Message));
        }
    }


    public LuceneDiagnosticsPage GetDiagnosticsPage(LuceneIndex index, Guid runId, LuceneDiagnosticCategory category, int skip, int take)
    {
        var items = diagnosticsSnapshots.Get(runId, index.IndexName, category);
        if (items is null)
        {
            return new LuceneDiagnosticsPage(0, [], "These diagnostic results have expired. Run diagnostics again.");
        }

        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, LuceneIndexInspectorLimits.MaxPageSize);

        return new LuceneDiagnosticsPage(items.Count, items.Skip(skip).Take(take).ToList(), null);
    }


    private async Task<DiagnosticsClassification> ClassifyAsync(LuceneIndex index, CancellationToken cancellationToken)
    {
        var strategy = serviceProvider.GetRequiredStrategy(index);
        var selected = await itemEnumerator.GetItemsAsync(index, cancellationToken);

        var storedKeys = new HashSet<(Guid, string)>();
        var storedDocs = new List<(Guid Guid, string Language, string Type)>();
        if (index.StorageContext.TryGetPublishedIndex() is not null)
        {
            searchService.UseSearcher(index, searcher =>
            {
                int max = searcher.IndexReader.MaxDoc;
                var top = searcher.Search(new MatchAllDocsQuery(), Math.Max(1, max));
                foreach (var hit in top.ScoreDocs)
                {
                    var doc = searcher.Doc(hit.Doc);
                    if (Guid.TryParse(doc.Get(BaseDocumentProperties.ITEM_GUID), out var guid))
                    {
                        string language = doc.Get(BaseDocumentProperties.LANGUAGE_NAME) ?? "";
                        storedKeys.Add((guid, language.ToLowerInvariant()));
                        storedDocs.Add((guid, language, doc.Get(BaseDocumentProperties.CONTENT_TYPE_NAME) ?? ""));
                    }
                }

                return 0;
            });
        }

        var skipped = new List<LuceneDiagnosticItem>();
        var missing = new List<LuceneDiagnosticItem>();
        var selectedKeys = new HashSet<(Guid, string)>();
        int indexable = 0;

        foreach (var item in selected)
        {
            cancellationToken.ThrowIfCancellationRequested();
            // The enumerator can return one document more than once (language fallback, overlapping paths); classify it once.
            if (!selectedKeys.Add((item.ItemGuid, item.LanguageName.ToLowerInvariant())))
            {
                continue;
            }

            var document = await strategy.MapToLuceneDocumentOrNull(item);
            if (document is null)
            {
                skipped.Add(ToDiagnostic(item, item.IsSecured
                    ? "Skipped by the indexing strategy. The item is secured."
                    : "Skipped by the indexing strategy (it returned no document)."));

                continue;
            }

            indexable++;
            if (!storedKeys.Contains((item.ItemGuid, item.LanguageName.ToLowerInvariant())))
            {
                missing.Add(ToDiagnostic(item, "Indexable but not found in the published generation. Indexing may be pending or the index is out of date."));
            }
        }

        var orphans = storedDocs
            .Where(d => !selectedKeys.Contains((d.Guid, d.Language.ToLowerInvariant())))
            .Select(d => new LuceneDiagnosticItem(d.Guid, d.Language, d.Type, "", "Stored in the index but not selected by the current configuration."))
            .ToList();

        return new DiagnosticsClassification(selectedKeys.Count, indexable, Sort(skipped), Sort(missing), Sort(orphans));
    }


    // Stable order, so pages do not overlap or skip rows.
    private static List<LuceneDiagnosticItem> Sort(IEnumerable<LuceneDiagnosticItem> items) =>
        items
            .OrderBy(i => i.ContentTypeName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(i => i.LanguageName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(i => i.ItemGuid)
            .ToList();


    private sealed record DiagnosticsClassification(
        int SelectedItemCount,
        int IndexableCount,
        IReadOnlyList<LuceneDiagnosticItem> Skipped,
        IReadOnlyList<LuceneDiagnosticItem> Missing,
        IReadOnlyList<LuceneDiagnosticItem> Orphans)
    {
        public LuceneIndexDiagnostics ToDiagnostics(int rows) => new(
            SelectedItemCount,
            IndexableCount,
            Skipped.Count,
            Missing.Count,
            Orphans.Count,
            Skipped.Take(rows).ToList(),
            Missing.Take(rows).ToList(),
            Orphans.Take(rows).ToList(),
            null);
    }


    private static LuceneDiagnosticItem ToDiagnostic(IIndexEventItemModel item, string reason) =>
        new(item.ItemGuid, item.LanguageName, item.ContentTypeName, item.Name, reason);


    private static LuceneQueryTestResult Search(IndexSearcher searcher, Query query, int skip, int take)
    {
        var top = searcher.Search(query, skip + take);
        var hits = top.ScoreDocs
            .Skip(skip)
            .Select(hit => new LuceneQueryHit(
                ToStoredDocument(searcher, hit.Doc),
                hit.Score,
                searcher.Explain(query, hit.Doc).ToString()))
            .ToList();

        return new LuceneQueryTestResult(query.ToString(), top.TotalHits, hits, null);
    }


    private Dictionary<string, LuceneSearchFieldDescriptor> GetDescriptors(LuceneIndex index)
    {
        var descriptors = new Dictionary<string, LuceneSearchFieldDescriptor>(StringComparer.Ordinal);
        foreach (var descriptor in LuceneSearchFieldDescriptor.BaseDocumentFields
            .Concat(serviceProvider.GetRequiredStrategy(index).GetSearchFieldDescriptors() ?? []))
        {
            if (!string.IsNullOrWhiteSpace(descriptor?.Name))
            {
                descriptors[descriptor.Name] = descriptor;
            }
        }

        return descriptors;
    }


    private static List<LuceneSearchField> GetSearchFields(IndexSearcher searcher, IReadOnlyDictionary<string, LuceneSearchFieldDescriptor> descriptors)
    {
        // Facet fields are indexed under internal "$" names and are searched through facets, not filters.
        var indexed = MultiFields.GetMergedFieldInfos(searcher.IndexReader)
            .Where(f => f.IsIndexed && !f.Name.StartsWith('$'))
            .Select(f => f.Name)
            .ToHashSet(StringComparer.Ordinal);

        var typed = descriptors.Values
            .Where(d => indexed.Contains(d.Name))
            .Select(d => new LuceneSearchField(d.Name, string.IsNullOrWhiteSpace(d.DisplayName) ? d.Name : d.DisplayName, d.Kind));
        var untyped = indexed
            .Where(name => !descriptors.ContainsKey(name))
            .Order(StringComparer.Ordinal)
            .Select(name => new LuceneSearchField(name, name, null));

        return [.. typed, .. untyped];
    }


    private static (Query? Query, string? Error) BuildFilterQuery(LuceneIndex index, LuceneSearchFieldDescriptor descriptor, string value)
    {
        string label = string.IsNullOrWhiteSpace(descriptor.DisplayName) ? descriptor.Name : descriptor.DisplayName;
        string trimmed = value.Trim();
        if (trimmed.Length == 0)
        {
            return (null, $"Enter a value for {label}.");
        }

        switch (descriptor.Kind)
        {
            case LuceneSearchFieldKind.Text when HasWildcard(trimmed):
                return BuildTextWildcardQuery(index, descriptor.Name, label, trimmed);

            case LuceneSearchFieldKind.Text:
                var text = new QueryBuilder(index.LuceneAnalyzer).CreateBooleanQuery(descriptor.Name, trimmed, Occur.MUST);
                return text is null
                    ? (null, $"The analyzer removed every term from the {label} filter. Enter other words.")
                    : (text, null);

            case LuceneSearchFieldKind.Exact when HasWildcard(trimmed):
                return (BuildExactWildcardQuery(descriptor.Name, trimmed), null);

            case LuceneSearchFieldKind.Exact:
                return (new TermQuery(new Term(descriptor.Name, trimmed)), null);

            case LuceneSearchFieldKind.Boolean:
                if (string.Equals(trimmed, "true", StringComparison.OrdinalIgnoreCase))
                {
                    return (new TermQuery(new Term(descriptor.Name, descriptor.TrueValue)), null);
                }

                if (string.Equals(trimmed, "false", StringComparison.OrdinalIgnoreCase))
                {
                    return (new TermQuery(new Term(descriptor.Name, descriptor.FalseValue)), null);
                }

                return (null, $"{label} accepts only true or false.");

            default:
                return (null, $"{label} has an unsupported kind.");
        }
    }


    private static bool HasWildcard(string value) => value.IndexOfAny(['*', '?']) >= 0;


    /// <summary>
    /// Matches the whole indexed term of a keyword field. A single trailing <c>*</c> becomes a prefix query;
    /// other patterns become a wildcard query, where <c>\</c> escapes a literal <c>*</c>, <c>?</c> or <c>\</c>.
    /// Case is preserved and the value is never analyzed.
    /// </summary>
    private static Query BuildExactWildcardQuery(string field, string pattern)
    {
        string prefix = pattern[..^1];
        if (pattern[^1] == '*' && !HasWildcard(prefix) && !prefix.Contains('\\'))
        {
            return new PrefixQuery(new Term(field, prefix));
        }

        return new WildcardQuery(new Term(field, pattern));
    }


    /// <summary>
    /// Parses analyzed text that contains wildcards with the classic parser bound to one field.
    /// Every query-syntax character except <c>*</c> and <c>?</c> is escaped, so a value cannot target
    /// another field or add operators. Wildcard terms are lowercased instead of analyzed, as the classic parser does.
    /// </summary>
    private static (Query? Query, string? Error) BuildTextWildcardQuery(LuceneIndex index, string field, string label, string value)
    {
        var escaped = new System.Text.StringBuilder(value.Length * 2);
        foreach (string word in value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        {
            if (escaped.Length > 0)
            {
                escaped.Append(' ');
            }

            if (word is "AND" or "OR" or "NOT")
            {
                escaped.Append('\\');
            }

            foreach (char c in word)
            {
                if (c is not ('*' or '?') && "\\+-!():^[]\"{}~|&/".Contains(c))
                {
                    escaped.Append('\\');
                }

                escaped.Append(c);
            }
        }

        var parser = new QueryParser(AnalyzerStorage.AnalyzerLuceneVersion, field, index.LuceneAnalyzer)
        {
            AllowLeadingWildcard = true,
            DefaultOperator = Operator.AND,
        };

        try
        {
            var query = parser.Parse(escaped.ToString());
            return query is BooleanQuery { Clauses.Count: 0 }
                ? (null, $"The analyzer removed every term from the {label} filter. Enter other words.")
                : (query, null);
        }
        catch (ParseException ex)
        {
            return (null, $"The {label} filter could not be parsed: {ex.Message}");
        }
    }


    private static LuceneStoredDocument ToStoredDocument(IndexSearcher searcher, int docId)
    {
        var document = searcher.Doc(docId);
        var fields = document.Fields
            .Select(f => new LuceneStoredField(f.Name, f.GetStringValue() ?? ""))
            .ToList();

        return new LuceneStoredDocument(docId, fields);
    }


    private static DateTime? GetLastWriteTimeUtc(string path)
    {
        try
        {
            return Directory.Exists(path) ? Directory.GetLastWriteTimeUtc(path) : null;
        }
        catch (IOException)
        {
            return null;
        }
    }
}
