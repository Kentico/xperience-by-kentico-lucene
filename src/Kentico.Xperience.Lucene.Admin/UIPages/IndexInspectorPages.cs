using CMS.Core;
using CMS.DataEngine;
using CMS.Membership;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Lucene.Admin;
using Kentico.Xperience.Lucene.Core.Indexing;

[assembly: UIPage(
   parentType: typeof(IndexEditSection),
   slug: "overview",
   uiPageType: typeof(IndexOverviewPage),
   name: "Overview",
   templateName: IndexInspectorPageBase.TEMPLATE,
   order: 100)]
[assembly: UIPage(
   parentType: typeof(IndexEditSection),
   slug: "search",
   uiPageType: typeof(IndexSearchPage),
   name: "Search",
   templateName: IndexInspectorPageBase.TEMPLATE,
   order: 300)]

namespace Kentico.Xperience.Lucene.Admin;

/// <summary>
/// Client properties shared by the inspector tabs.
/// </summary>
public sealed class IndexInspectorClientProperties : TemplateClientProperties
{
    /// <summary>The tab the page renders: overview or search.</summary>
    public string Tab { get; set; } = "";
}


/// <summary>Index summary returned to the Overview tab.</summary>
public sealed record IndexSummaryResult(
    string IndexName,
    string DisplayName,
    string StrategyName,
    string AnalyzerName,
    IReadOnlyList<string> Languages,
    IReadOnlyList<string> ReusableContentTypes,
    IReadOnlyList<IndexChannelSummary> Channels,
    LuceneIndexStatus Status,
    IReadOnlyDictionary<string, string>? ContentTypeDisplayNames = null);


/// <summary>Channel and its included paths.</summary>
public sealed record IndexChannelSummary(string ChannelName, IReadOnlyList<IndexPathSummary> Paths);


/// <summary>Included path and its content types.</summary>
public sealed record IndexPathSummary(string AliasPath, IReadOnlyList<string> ContentTypes);


/// <summary>Arguments for loading stored documents.</summary>
public sealed class DocumentsArguments
{
    public int Skip { get; set; }
    public int Take { get; set; } = 20;
}


/// <summary>Arguments for a test query.</summary>
public sealed class TestQueryArguments
{
    public string Query { get; set; } = "";
    public string Field { get; set; } = "";
    public int Skip { get; set; }
    public int Take { get; set; } = 10;
}


/// <summary>A filter on one typed field.</summary>
public sealed class SearchFilterArgument
{
    public string Field { get; set; } = "";
    public string Value { get; set; } = "";
}


/// <summary>Arguments for a typed filter query. Every filter must match.</summary>
public sealed class FilterQueryArguments
{
    public List<SearchFilterArgument> Filters { get; set; } = [];
    public int Skip { get; set; }
    public int Take { get; set; } = 10;
}


/// <param name="Name">Lucene field name.</param>
/// <param name="DisplayName">Label from the field descriptor, or the field name.</param>
/// <param name="Kind"><c>text</c>, <c>exact</c>, <c>boolean</c>, or <c>untyped</c> when no descriptor declares the field.</param>
public sealed record SearchFieldResult(string Name, string DisplayName, string Kind);


/// <param name="Fields">Indexed fields, typed fields first.</param>
/// <param name="Error">Set when the fields could not be read.</param>
public sealed record SearchFieldsResult(IReadOnlyList<SearchFieldResult> Fields, string? Error);


/// <summary>Arguments for starting a pageable diagnostics run.</summary>
public sealed class DiagnosticsRunArguments
{
    public int Take { get; set; } = 20;
}


/// <summary>Arguments for loading one page of a diagnostics category.</summary>
public sealed class DiagnosticsPageArguments
{
    public Guid RunId { get; set; }

    /// <summary><c>skipped</c>, <c>missing</c> or <c>orphans</c>.</summary>
    public string Category { get; set; } = "";
    public int Skip { get; set; }
    public int Take { get; set; } = 20;
}


/// <param name="Requested">Whether the rebuild was requested. Does not mean it finished.</param>
/// <param name="Error">Set when the rebuild could not be requested.</param>
public sealed record IndexRebuildResult(bool Requested, string? Error);


/// <param name="CanRebuild">Whether the current user has the Rebuild permission.</param>
public sealed record IndexRebuildAccessResult(bool CanRebuild);


/// <summary>
/// Base of the read-only index inspector tabs. Commands here never modify an index.
/// </summary>
[UIEvaluatePermission(SystemPermissions.VIEW)]
public abstract class IndexInspectorPageBase : Page<IndexInspectorClientProperties>
{
    internal const string TEMPLATE = "@kentico/xperience-integrations-lucene-admin/IndexInspector";

    private readonly ILuceneIndexManager indexManager;
    private readonly ILuceneConfigurationStorageService storageService;
    private readonly ILuceneIndexInspector inspector;

    [PageParameter(typeof(IntPageModelBinder), typeof(IndexEditSection))]
    public int IndexIdentifier { get; set; }

    protected abstract string Tab { get; }


    protected IndexInspectorPageBase(
        ILuceneIndexManager indexManager,
        ILuceneConfigurationStorageService storageService,
        ILuceneIndexInspector inspector)
    {
        this.indexManager = indexManager;
        this.storageService = storageService;
        this.inspector = inspector;
    }


    public override Task<IndexInspectorClientProperties> ConfigureTemplateProperties(IndexInspectorClientProperties properties)
    {
        properties.Tab = Tab;

        return Task.FromResult(properties);
    }


    [PageCommand(Permission = SystemPermissions.VIEW)]
    public async Task<ICommandResponse<IndexSummaryResult>> LoadSummary()
    {
        var index = indexManager.GetIndex(IndexIdentifier);
        var model = await storageService.GetIndexDataOrNullAsync(IndexIdentifier);
        if (index is null || model is null)
        {
            return ResponseFrom(new IndexSummaryResult("", "", "", "", [], [], [], new LuceneIndexStatus(false, null, [], 0, 0, null, null, null)))
                .AddErrorMessage("The index was not found.");
        }

        var channels = model.Channels
            .Select(c => new IndexChannelSummary(
                c.WebsiteChannelName,
                c.IncludedPaths.Select(p => new IndexPathSummary(p.AliasPath, p.ContentTypes.Select(t => t.ContentTypeName).ToList())).ToList()))
            .ToList();

        var contentTypeNames = model.ReusableContentTypeNames
            .Concat(channels.SelectMany(c => c.Paths).SelectMany(p => p.ContentTypes))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        // Types deleted from Xperience are left out, so the client shows their code name.
        var contentTypeDisplayNames = contentTypeNames.Count == 0
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : (await DataClassInfoProvider.ProviderObject.Get()
                .WhereIn(nameof(DataClassInfo.ClassName), contentTypeNames)
                .Columns(nameof(DataClassInfo.ClassName), nameof(DataClassInfo.ClassDisplayName))
                .GetEnumerableTypedResultAsync())
                .Where(c => !string.IsNullOrWhiteSpace(c.ClassDisplayName))
                .GroupBy(c => c.ClassName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().ClassDisplayName, StringComparer.OrdinalIgnoreCase);

        return ResponseFrom(new IndexSummaryResult(
            index.IndexName,
            index.DisplayName,
            model.StrategyName,
            model.AnalyzerName,
            model.LanguageNames.ToList(),
            model.ReusableContentTypeNames.ToList(),
            channels,
            inspector.GetStatus(index),
            contentTypeDisplayNames));
    }


    [PageCommand(Permission = SystemPermissions.VIEW)]
    public Task<ICommandResponse<LuceneStoredDocumentsPage>> LoadDocuments(DocumentsArguments arguments)
    {
        var index = indexManager.GetIndex(IndexIdentifier);
        var result = index is null
            ? new LuceneStoredDocumentsPage(0, [], "The index was not found.")
            : inspector.GetDocuments(index, arguments.Skip, arguments.Take);

        return Task.FromResult(ResponseFrom(result));
    }


    [PageCommand(Permission = SystemPermissions.VIEW)]
    public Task<ICommandResponse<LuceneQueryTestResult>> RunQuery(TestQueryArguments arguments)
    {
        var index = indexManager.GetIndex(IndexIdentifier);
        var result = index is null
            ? new LuceneQueryTestResult(null, 0, [], "The index was not found.")
            : inspector.TestQuery(index, arguments.Query, arguments.Field, arguments.Skip, arguments.Take);

        return Task.FromResult(ResponseFrom(result));
    }


    [PageCommand(Permission = SystemPermissions.VIEW)]
    public Task<ICommandResponse<SearchFieldsResult>> LoadSearchFields()
    {
        var index = indexManager.GetIndex(IndexIdentifier);
        if (index is null)
        {
            return Task.FromResult(ResponseFrom(new SearchFieldsResult([], "The index was not found.")));
        }

        var result = inspector.GetSearchFields(index);
        var fields = result.Fields
            .Select(f => new SearchFieldResult(f.Name, f.DisplayName, f.Kind switch
            {
                LuceneSearchFieldKind.Text => "text",
                LuceneSearchFieldKind.Exact => "exact",
                LuceneSearchFieldKind.Boolean => "boolean",
                _ => "untyped",
            }))
            .ToList();

        return Task.FromResult(ResponseFrom(new SearchFieldsResult(fields, result.Error)));
    }


    [PageCommand(Permission = SystemPermissions.VIEW)]
    public Task<ICommandResponse<LuceneQueryTestResult>> RunFilterQuery(FilterQueryArguments arguments)
    {
        var index = indexManager.GetIndex(IndexIdentifier);
        var filters = (arguments.Filters ?? [])
            .Select(f => new LuceneSearchFilter(f.Field ?? "", f.Value ?? ""))
            .ToList();
        var result = index is null
            ? new LuceneQueryTestResult(null, 0, [], "The index was not found.")
            : inspector.FilterQuery(index, filters, arguments.Skip, arguments.Take);

        return Task.FromResult(ResponseFrom(result));
    }


    [PageCommand(Permission = SystemPermissions.VIEW)]
    public async Task<ICommandResponse<LuceneIndexDiagnostics>> RunDiagnostics()
    {
        var index = indexManager.GetIndex(IndexIdentifier);
        var result = index is null
            ? new LuceneIndexDiagnostics(0, 0, 0, 0, 0, [], [], [], "The index was not found.")
            : await inspector.GetDiagnosticsAsync(index, CancellationToken.None);

        return ResponseFrom(result);
    }


    [PageCommand(Permission = SystemPermissions.VIEW)]
    public async Task<ICommandResponse<LuceneDiagnosticsRun>> StartDiagnostics(DiagnosticsRunArguments arguments)
    {
        var index = indexManager.GetIndex(IndexIdentifier);
        var result = index is null
            ? new LuceneDiagnosticsRun(null, new LuceneIndexDiagnostics(0, 0, 0, 0, 0, [], [], [], "The index was not found."))
            : await inspector.RunDiagnosticsAsync(index, arguments.Take, CancellationToken.None);

        return ResponseFrom(result);
    }


    [PageCommand(Permission = SystemPermissions.VIEW)]
    public Task<ICommandResponse<LuceneDiagnosticsPage>> LoadDiagnosticsPage(DiagnosticsPageArguments arguments)
    {
        var index = indexManager.GetIndex(IndexIdentifier);
        if (index is null)
        {
            return Task.FromResult(ResponseFrom(new LuceneDiagnosticsPage(0, [], "The index was not found.")));
        }

        var result = Enum.TryParse<LuceneDiagnosticCategory>(arguments.Category, true, out var category) && Enum.IsDefined(category)
            ? inspector.GetDiagnosticsPage(index, arguments.RunId, category, arguments.Skip, arguments.Take)
            : new LuceneDiagnosticsPage(0, [], "Unknown diagnostics category.");

        return Task.FromResult(ResponseFrom(result));
    }
}


/// <summary>
/// Overview tab. Besides the read-only commands, it can request a rebuild for users with the Rebuild permission.
/// </summary>
public sealed class IndexOverviewPage : IndexInspectorPageBase
{
    private readonly ILuceneIndexManager indexManager;
    private readonly ILuceneClient luceneClient;
    private readonly IUIPermissionEvaluator permissionEvaluator;

    protected override string Tab => "overview";

    public IndexOverviewPage(
        ILuceneIndexManager indexManager,
        ILuceneConfigurationStorageService storageService,
        ILuceneIndexInspector inspector,
        ILuceneClient luceneClient,
        IUIPermissionEvaluator permissionEvaluator)
        : base(indexManager, storageService, inspector)
    {
        this.indexManager = indexManager;
        this.luceneClient = luceneClient;
        this.permissionEvaluator = permissionEvaluator;
    }


    [PageCommand(Permission = SystemPermissions.VIEW)]
    public async Task<ICommandResponse<IndexRebuildAccessResult>> LoadRebuildAccess()
    {
        var access = await permissionEvaluator.Evaluate(LuceneIndexPermissions.REBUILD);

        return ResponseFrom(new IndexRebuildAccessResult(access.Succeeded));
    }


    /// <summary>
    /// Requests a rebuild like the listing's Rebuild action. Completion is not tracked.
    /// </summary>
    [PageCommand(Permission = LuceneIndexPermissions.REBUILD)]
    public async Task<ICommandResponse<IndexRebuildResult>> RebuildIndex()
    {
        var index = indexManager.GetIndex(IndexIdentifier);
        if (index is null)
        {
            return ResponseFrom(new IndexRebuildResult(false, "The index is not loaded in this application. Reload the page; if the index still exists, check the Event Log."));
        }

        try
        {
            // Not cancelled when the user leaves the page, matching the listing action.
            await luceneClient.Rebuild(index.IndexName, CancellationToken.None);

            return ResponseFrom(new IndexRebuildResult(true, null));
        }
        catch (Exception ex)
        {
            EventLogService.LogException(nameof(IndexOverviewPage), nameof(RebuildIndex), ex);

            return ResponseFrom(new IndexRebuildResult(false, string.Format("Errors occurred while rebuilding the '{0}' index. Please check the Event Log for more details.", index.IndexName)));
        }
    }
}


/// <summary>
/// Browses stored documents when the query is blank and runs test queries otherwise.
/// </summary>
public sealed class IndexSearchPage : IndexInspectorPageBase
{
    protected override string Tab => "search";

    public IndexSearchPage(ILuceneIndexManager indexManager, ILuceneConfigurationStorageService storageService, ILuceneIndexInspector inspector)
        : base(indexManager, storageService, inspector) { }
}
