using System.Text.RegularExpressions;

using CMS.ContentEngine;
using CMS.Core;
using CMS.DataEngine;
using CMS.Membership;
using CMS.Websites;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Lucene.Admin;
using Kentico.Xperience.Lucene.Core.Indexing;

[assembly: UIPage(
   parentType: typeof(IndexListingPage),
   slug: "create",
   uiPageType: typeof(IndexCreatePage),
   name: "Create index",
   templateName: "@kentico/xperience-integrations-lucene-admin/IndexWizard",
   order: UIPageOrder.NoOrder)]

namespace Kentico.Xperience.Lucene.Admin;

public sealed record WizardOption(string Value, string Label);


/// <summary>Options and rules the create wizard needs.</summary>
public sealed class IndexWizardClientProperties : TemplateClientProperties
{
    public List<WizardOption> Channels { get; set; } = [];
    public List<WizardOption> WebContentTypes { get; set; } = [];
    public List<WizardOption> ReusableContentTypes { get; set; } = [];
    public List<WizardOption> Languages { get; set; } = [];
    public List<string> Analyzers { get; set; } = [];
    public List<string> Strategies { get; set; } = [];
    public List<string> ExistingIndexNames { get; set; } = [];
}


public sealed class WizardPath
{
    public string AliasPath { get; set; } = "";
    public List<string> ContentTypes { get; set; } = [];
}


public sealed class WizardChannel
{
    public string ChannelName { get; set; } = "";
    public List<WizardPath> Paths { get; set; } = [];
}


public sealed class CreateIndexArguments
{
    public string DisplayName { get; set; } = "";
    public string IndexName { get; set; } = "";
    public List<WizardChannel> Channels { get; set; } = [];
    public List<string> ReusableContentTypes { get; set; } = [];
    public List<string> Languages { get; set; } = [];
    public string AnalyzerName { get; set; } = "";
    public string StrategyName { get; set; } = "";
}


public sealed class PathPreviewArguments
{
    public string ChannelName { get; set; } = "";
    public string AliasPath { get; set; } = "";
    public List<string> ContentTypes { get; set; } = [];
    public string Language { get; set; } = "";
}


public sealed record PathPreviewItem(string Name, string Path, string ContentType);


public sealed record PathPreviewResult(int Total, bool IsWildcard, IReadOnlyList<PathPreviewItem> Items, string? Error);


public sealed record CreateIndexResult(bool Success, int IndexId, IReadOnlyList<string> Errors);


public sealed record OpenIndexArguments(int IndexId);


/// <summary>
/// Guided flow for creating an index: identity, content, languages and analyzer, strategy, review.
/// </summary>
[UIEvaluatePermission(SystemPermissions.CREATE)]
public partial class IndexCreatePage : Page<IndexWizardClientProperties>
{
    private const int PREVIEW_SIZE = 10;

    private readonly ILuceneIndexManager indexManager;
    private readonly IPageLinkGenerator pageLinkGenerator;
    private readonly IContentQueryExecutor executor;
    private readonly IInfoProvider<ChannelInfo> channelProvider;
    private readonly IInfoProvider<ContentLanguageInfo> languageProvider;


    public IndexCreatePage(
        ILuceneIndexManager indexManager,
        IPageLinkGenerator pageLinkGenerator,
        IContentQueryExecutor executor,
        IInfoProvider<ChannelInfo> channelProvider,
        IInfoProvider<ContentLanguageInfo> languageProvider)
    {
        this.indexManager = indexManager;
        this.pageLinkGenerator = pageLinkGenerator;
        this.executor = executor;
        this.channelProvider = channelProvider;
        this.languageProvider = languageProvider;
    }


    public override async Task<IndexWizardClientProperties> ConfigureTemplateProperties(IndexWizardClientProperties properties)
    {
        var channels = await channelProvider.Get()
            .WhereEquals(nameof(ChannelInfo.ChannelType), ChannelType.Website.ToString())
            .GetEnumerableTypedResultAsync(cancellationToken: CancellationToken.None);
        var languages = await languageProvider.Get()
            .GetEnumerableTypedResultAsync(cancellationToken: CancellationToken.None);
        var classes = (await DataClassInfoProvider.ProviderObject.Get()
            .GetEnumerableTypedResultAsync(cancellationToken: CancellationToken.None))
            .ToList();

        properties.Channels = channels.Select(c => new WizardOption(c.ChannelName, c.ChannelDisplayName)).ToList();
        properties.Languages = languages.Select(l => new WizardOption(l.ContentLanguageName, l.ContentLanguageDisplayName)).ToList();
        properties.WebContentTypes = classes
            .Where(c => c.ClassContentTypeType == "Website")
            .Select(c => new WizardOption(c.ClassName, c.ClassDisplayName))
            .ToList();
        properties.ReusableContentTypes = classes
            .Where(c => c.ClassContentTypeType == "Reusable")
            .Select(c => new WizardOption(c.ClassName, c.ClassDisplayName))
            .ToList();
        properties.Analyzers = AnalyzerStorage.Analyzers.Keys.ToList();
        properties.Strategies = StrategyStorage.Strategies.Keys.ToList();
        properties.ExistingIndexNames = indexManager.GetAllIndices().Select(i => i.IndexName).ToList();

        return properties;
    }


    /// <summary>
    /// Lists the pages a path selects so the wildcard behavior can be confirmed before the index is saved.
    /// </summary>
    [PageCommand(Permission = SystemPermissions.CREATE)]
    public async Task<ICommandResponse<PathPreviewResult>> PreviewPath(PathPreviewArguments arguments)
    {
        if (string.IsNullOrWhiteSpace(arguments.AliasPath) || arguments.ContentTypes.Count == 0
            || string.IsNullOrWhiteSpace(arguments.ChannelName) || string.IsNullOrWhiteSpace(arguments.Language))
        {
            return ResponseFrom(new PathPreviewResult(0, false, [], "Select a channel, path, content type and language to preview."));
        }

        bool wildcard = LucenePathHelper.EndsWithWildcard(arguments.AliasPath);
        var pathMatch = wildcard
            ? PathMatch.Children(arguments.AliasPath[..^2])
            : PathMatch.Single(arguments.AliasPath);

        try
        {
            var builder = new ContentItemQueryBuilder();
            foreach (string type in arguments.ContentTypes)
            {
                builder.ForContentType(type, config => config.ForWebsite(arguments.ChannelName, includeUrlPath: true, pathMatch: pathMatch));
            }

            builder.InLanguage(arguments.Language);

            var pages = (await executor.GetWebPageResult(builder, container => container, cancellationToken: CancellationToken.None)).ToList();
            var items = pages
                .Take(PREVIEW_SIZE)
                .Select(p => new PathPreviewItem(p.WebPageItemName, p.WebPageItemTreePath, p.ContentTypeName))
                .ToList();

            return ResponseFrom(new PathPreviewResult(pages.Count, wildcard, items, null));
        }
        catch (Exception ex)
        {
            EventLogService.LogException(nameof(IndexCreatePage), nameof(PreviewPath), ex);

            return ResponseFrom(new PathPreviewResult(0, wildcard, [], "The preview could not be loaded. Check the Event Log for details."));
        }
    }


    [PageCommand(Permission = SystemPermissions.CREATE)]
    public async Task<ICommandResponse<CreateIndexResult>> CreateIndex(CreateIndexArguments arguments)
    {
        var knownLanguages = (await languageProvider.Get().GetEnumerableTypedResultAsync(cancellationToken: CancellationToken.None))
            .Select(l => l.ContentLanguageName)
            .ToList();

        var errors = Validate(
            arguments,
            indexManager.GetAllIndices().Select(i => i.IndexName).ToList(),
            knownLanguages,
            AnalyzerStorage.Analyzers.Keys.ToList(),
            StrategyStorage.Strategies.Keys.ToList());

        if (errors.Count > 0)
        {
            return ResponseFrom(new CreateIndexResult(false, 0, errors));
        }

        var channelNames = (await channelProvider.Get().GetEnumerableTypedResultAsync(cancellationToken: CancellationToken.None))
            .ToDictionary(c => c.ChannelName, c => c.ChannelDisplayName);

        var model = new LuceneIndexModel
        {
            IndexName = arguments.IndexName.Trim(),
            DisplayName = arguments.DisplayName.Trim(),
            LanguageNames = arguments.Languages,
            AnalyzerName = arguments.AnalyzerName,
            StrategyName = arguments.StrategyName,
            ReusableContentTypeNames = arguments.ReusableContentTypes,
            Channels = arguments.Channels.Select(c => new LuceneIndexChannelConfiguration(
                c.ChannelName,
                channelNames.GetValueOrDefault(c.ChannelName, c.ChannelName),
                c.Paths.Select(p => new LuceneIndexIncludedPath(p.AliasPath.Trim())
                {
                    ContentTypes = p.ContentTypes.Select(t => new LuceneIndexContentType(t, t, 0)).ToList()
                }).ToList()))
        };

        try
        {
            indexManager.AddIndex(model);
        }
        catch (Exception ex)
        {
            EventLogService.LogException(nameof(IndexCreatePage), nameof(CreateIndex), ex);

            return ResponseFrom(new CreateIndexResult(false, 0, ["The index could not be created. Check the Event Log for details."]))
                .AddErrorMessage("Could not create index.");
        }

        var created = indexManager.GetIndex(model.IndexName);

        return ResponseFrom(new CreateIndexResult(created is not null, created?.Identifier ?? 0, []));
    }


    [PageCommand(Permission = SystemPermissions.CREATE)]
    public Task<ICommandResponse> OpenIndex(OpenIndexArguments arguments)
    {
        var values = new PageParameterValues { { typeof(IndexEditSection), arguments.IndexId } };
        var response = NavigateTo(pageLinkGenerator.GetPath<IndexEditSection>(values))
            .AddSuccessMessage("Index created.");

        return Task.FromResult<ICommandResponse>(response);
    }


    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9_.\\-]*$")]
    private static partial Regex CodeNameRegex();


    internal static List<string> Validate(
        CreateIndexArguments arguments,
        IReadOnlyCollection<string> existingNames,
        IReadOnlyCollection<string> knownLanguages,
        IReadOnlyCollection<string> analyzers,
        IReadOnlyCollection<string> strategies)
    {
        var errors = new List<string>();
        string code = arguments.IndexName?.Trim() ?? "";

        if (!CodeNameRegex().IsMatch(code))
        {
            errors.Add("The code name is required and may contain only letters, numbers, '_', '-' and '.'.");
        }
        else if (existingNames.Contains(code, StringComparer.OrdinalIgnoreCase))
        {
            errors.Add($"An index with the code name '{code}' already exists.");
        }

        bool hasWebContent = arguments.Channels.Count > 0
            && arguments.Channels.All(c => c.Paths.Count > 0 && c.Paths.All(p => !string.IsNullOrWhiteSpace(p.AliasPath) && p.ContentTypes.Count > 0));
        bool hasAnyChannel = arguments.Channels.Count > 0;

        if (!hasAnyChannel && arguments.ReusableContentTypes.Count == 0)
        {
            errors.Add("Select at least one channel with a path and content type, or a reusable content type.");
        }
        else if (hasAnyChannel && !hasWebContent)
        {
            errors.Add("Every selected channel needs at least one path, and every path at least one content type.");
        }

        if (arguments.Channels.Any(c => c.Paths.Any(p => !string.IsNullOrWhiteSpace(p.AliasPath) && !p.AliasPath.Trim().StartsWith('/'))))
        {
            errors.Add("Paths must start with '/', for example /Articles/%.");
        }

        if (arguments.Channels.Any(c => c.Paths.Select(p => p.AliasPath.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != c.Paths.Count))
        {
            errors.Add("A channel cannot include the same path more than once.");
        }

        if (arguments.Channels.Select(c => c.ChannelName).Distinct(StringComparer.OrdinalIgnoreCase).Count() != arguments.Channels.Count)
        {
            errors.Add("Each channel can be added only once.");
        }

        if (arguments.Languages.Count == 0 || arguments.Languages.Any(l => !knownLanguages.Contains(l)))
        {
            errors.Add("Select at least one existing language.");
        }

        if (!analyzers.Contains(arguments.AnalyzerName))
        {
            errors.Add("Select a registered analyzer.");
        }

        if (!strategies.Contains(arguments.StrategyName))
        {
            errors.Add("Select a registered indexing strategy.");
        }

        return errors;
    }
}
