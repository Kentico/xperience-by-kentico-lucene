using CMS.ContentEngine;
using CMS.Core;
using CMS.DataEngine;
using CMS.Membership;
using CMS.Membership.Internal;

using Kentico.Membership;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Authentication;
using Kentico.Xperience.Lucene.Admin;
using Kentico.Xperience.Lucene.Core.Indexing;

[assembly: UIPage(
   parentType: typeof(IndexEditSection),
   slug: "configuration",
   uiPageType: typeof(IndexEditPage),
   name: "Configuration",
   templateName: IndexEditPage.TEMPLATE,
   order: 200)]

namespace Kentico.Xperience.Lucene.Admin;

/// <summary>Client properties of the Configuration tab.</summary>
public sealed class IndexConfigurationClientProperties : TemplateClientProperties
{
    /// <summary>The stored configuration, or null when the index no longer exists.</summary>
    public IndexConfigurationData? Configuration { get; set; }
    public List<WizardOption> Strategies { get; set; } = [];
    public List<WizardOption> Analyzers { get; set; } = [];
    public List<WizardOption> Languages { get; set; } = [];
    public List<WizardOption> ReusableContentTypes { get; set; } = [];
    public List<WizardOption> WebContentTypes { get; set; } = [];

    /// <summary>Website channels the current user can view.</summary>
    public List<WizardOption> Channels { get; set; } = [];
}


/// <summary>Read-only view of an index configuration.</summary>
public sealed record IndexConfigurationData(
    int Id,
    string IndexName,
    string DisplayName,
    string StrategyName,
    string AnalyzerName,
    string RebuildHook,
    IReadOnlyList<string> LanguageNames,
    IReadOnlyList<WizardOption> ReusableContentTypes,
    IReadOnlyList<IndexConfigurationChannel> Channels);


/// <summary>Website channel of an index configuration.</summary>
public sealed record IndexConfigurationChannel(string WebsiteChannelName, string ChannelDisplayName, IReadOnlyList<IndexConfigurationPath> Paths);


/// <summary>Included path and its content types.</summary>
public sealed record IndexConfigurationPath(int? Identifier, string AliasPath, IReadOnlyList<WizardOption> ContentTypes);


/// <summary>Result of saving one configuration section.</summary>
public sealed record IndexConfigurationSaveResult(bool Saved, IReadOnlyList<string> Errors, IndexConfigurationData? Configuration);


/// <summary>Fields stored on the index record itself. The code name cannot be changed.</summary>
public sealed class IndexSettingsArguments
{
    public string DisplayName { get; set; } = "";
    public string StrategyName { get; set; } = "";
    public string AnalyzerName { get; set; } = "";
    public string RebuildHook { get; set; } = "";
}


public sealed class LanguagesSectionArguments
{
    public List<string> LanguageNames { get; set; } = [];
}

public sealed class ReusableSectionArguments
{
    public List<string> ContentTypeNames { get; set; } = [];
}


public sealed class ChannelSectionPath
{
    public int? Identifier { get; set; }
    public string AliasPath { get; set; } = "";
    public List<string> ContentTypes { get; set; } = [];
}


public sealed class ChannelSectionArguments
{
    public string WebsiteChannelName { get; set; } = "";

    /// <summary>True when the channel is added to the index rather than edited.</summary>
    public bool IsNew { get; set; }
    public List<ChannelSectionPath> Paths { get; set; } = [];
}


public sealed class RemoveChannelArguments
{
    public string WebsiteChannelName { get; set; } = "";
}


/// <summary>
/// Configuration tab. Shows the whole configuration read-only and saves one section at a time.
/// </summary>
[UIEvaluatePermission(SystemPermissions.UPDATE)]
public class IndexEditPage : Page<IndexConfigurationClientProperties>
{
    internal const string TEMPLATE = "@kentico/xperience-integrations-lucene-admin/IndexConfiguration";

    internal const string SavedMessage = "Index edited. Saving does not rebuild the index. Rebuild after changing channels, paths, content types, languages, the analyzer or the strategy.";

    private const string WebPagesApplicationPrefix = "Kentico.Xperience.Application.WebPages_";

    private readonly ILuceneConfigurationStorageService storageService;
    private readonly IInfoProvider<ChannelInfo> channelProvider;
    private readonly IInfoProvider<ContentLanguageInfo> languageProvider;
    private readonly IApplicationPermissionEvaluator permissionEvaluator;
    private readonly IAuthenticatedUserAccessor userAccessor;
    private readonly IEventLogService eventLogService;

    [PageParameter(typeof(IntPageModelBinder), typeof(IndexEditSection))]
    public int IndexIdentifier { get; set; }

    public IndexEditPage(
        ILuceneConfigurationStorageService storageService,
        IInfoProvider<ChannelInfo> channelProvider,
        IInfoProvider<ContentLanguageInfo> languageProvider,
        IApplicationPermissionEvaluator permissionEvaluator,
        IAuthenticatedUserAccessor userAccessor,
        IEventLogService eventLogService)
    {
        this.storageService = storageService;
        this.channelProvider = channelProvider;
        this.languageProvider = languageProvider;
        this.permissionEvaluator = permissionEvaluator;
        this.userAccessor = userAccessor;
        this.eventLogService = eventLogService;
    }


    public override async Task<IndexConfigurationClientProperties> ConfigureTemplateProperties(IndexConfigurationClientProperties properties)
    {
        var classes = await GetContentTypes();
        var user = await userAccessor.Get();
        var channels = await channelProvider.Get()
            .WhereEquals(nameof(ChannelInfo.ChannelType), ChannelType.Website.ToString())
            .GetEnumerableTypedResultAsync(cancellationToken: CancellationToken.None);
        var languages = await languageProvider.Get()
            .GetEnumerableTypedResultAsync(cancellationToken: CancellationToken.None);

        properties.Strategies = StrategyStorage.Strategies.Keys.Select(k => new WizardOption(k, k)).ToList();
        properties.Analyzers = AnalyzerStorage.Analyzers.Keys.Select(k => new WizardOption(k, k)).ToList();
        properties.Languages = languages.Select(l => new WizardOption(l.ContentLanguageName, l.ContentLanguageDisplayName)).ToList();
        properties.WebContentTypes = classes
            .Where(c => c.ClassContentTypeType == "Website")
            .Select(c => new WizardOption(c.ClassName, c.ClassDisplayName))
            .ToList();
        properties.ReusableContentTypes = classes
            .Where(c => c.ClassContentTypeType == "Reusable")
            .Select(c => new WizardOption(c.ClassName, c.ClassDisplayName))
            .ToList();
        properties.Channels = channels
            .Where(c => Allowed(c, user, SystemPermissions.VIEW))
            .Select(c => new WizardOption(c.ChannelName, c.ChannelDisplayName))
            .ToList();

        var stored = await storageService.GetIndexDataOrNullAsync(IndexIdentifier);
        properties.Configuration = stored is null ? null : ToData(stored, classes);

        return properties;
    }


    [PageCommand(Permission = SystemPermissions.UPDATE)]
    public Task<ICommandResponse<IndexConfigurationSaveResult>> SaveIndexSettings(IndexSettingsArguments arguments) =>
        Save(model =>
        {
            var errors = new List<string>();

            if (!StrategyStorage.Strategies.ContainsKey(arguments.StrategyName ?? ""))
            {
                errors.Add("Select a registered indexing strategy.");
            }

            if (!AnalyzerStorage.Analyzers.ContainsKey(arguments.AnalyzerName ?? ""))
            {
                errors.Add("Select a registered analyzer.");
            }

            model.DisplayName = arguments.DisplayName?.Trim() ?? "";
            model.StrategyName = arguments.StrategyName ?? "";
            model.AnalyzerName = arguments.AnalyzerName ?? "";
            model.RebuildHook = arguments.RebuildHook?.Trim() ?? "";

            return Task.FromResult<IEnumerable<string>>(errors);
        });


    [PageCommand(Permission = SystemPermissions.UPDATE)]
    public Task<ICommandResponse<IndexConfigurationSaveResult>> SaveLanguages(LanguagesSectionArguments arguments) =>
        Save(async model =>
        {
            var known = (await languageProvider.Get().GetEnumerableTypedResultAsync(cancellationToken: CancellationToken.None))
                .Select(l => l.ContentLanguageName)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var languages = (arguments.LanguageNames ?? []).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            model.LanguageNames = languages;

            return languages.Count == 0 || languages.Exists(l => !known.Contains(l))
                ? ["Select at least one existing language."]
                : [];
        });

    [PageCommand(Permission = SystemPermissions.UPDATE)]
    public Task<ICommandResponse<IndexConfigurationSaveResult>> SaveReusableContent(ReusableSectionArguments arguments) =>
        Save(async model =>
        {
            var submitted = (arguments.ContentTypeNames ?? []).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var known = (await GetContentTypes())
                .Where(c => c.ClassContentTypeType == "Reusable")
                .Select(c => c.ClassName)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Selected types that were deleted since may stay, but new ones must exist.
            var previous = model.ReusableContentTypeNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var unknown = submitted.Where(t => !known.Contains(t) && !previous.Contains(t)).ToList();

            model.ReusableContentTypeNames = submitted;

            return unknown.Select(t => $"The reusable content type {t} does not exist.");
        });


    [PageCommand(Permission = SystemPermissions.UPDATE)]
    public Task<ICommandResponse<IndexConfigurationSaveResult>> SaveChannel(ChannelSectionArguments arguments) =>
        Save(async model =>
        {
            string name = arguments.WebsiteChannelName?.Trim() ?? "";
            var channels = model.Channels.ToList();
            var existing = channels.Find(c => string.Equals(c.WebsiteChannelName, name, StringComparison.OrdinalIgnoreCase));

            if (existing is null && !arguments.IsNew)
            {
                return ["The website channel is no longer part of the index. Reload the page."];
            }

            if (existing is not null && arguments.IsNew)
            {
                return [$"The website channel {existing.ChannelDisplayName} is already part of the index."];
            }

            var channel = (await channelProvider.Get()
                .WhereEquals(nameof(ChannelInfo.ChannelName), name)
                .WhereEquals(nameof(ChannelInfo.ChannelType), ChannelType.Website.ToString())
                .GetEnumerableTypedResultAsync(cancellationToken: CancellationToken.None))
                .FirstOrDefault();

            if (channel is null)
            {
                return [$"The website channel {name} does not exist."];
            }

            var paths = (arguments.Paths ?? []).ToList();
            var errors = ValidateChannel(channel.ChannelDisplayName, paths).ToList();
            if (errors.Count > 0)
            {
                return errors;
            }

            var classes = (await GetContentTypes()).ToDictionary(c => c.ClassName, StringComparer.OrdinalIgnoreCase);
            var storedPaths = existing?.IncludedPaths.ToList() ?? [];
            var storedTypes = storedPaths
                .SelectMany(p => p.ContentTypes)
                .GroupBy(t => t.ContentTypeName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var included = new List<LuceneIndexIncludedPath>();
            foreach (var path in paths)
            {
                var types = new List<LuceneIndexContentType>();
                foreach (string type in path.ContentTypes.Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    if (classes.TryGetValue(type, out var info) && info.ClassContentTypeType == "Website")
                    {
                        types.Add(new LuceneIndexContentType(info.ClassName, info.ClassDisplayName, 0));
                    }
                    else if (storedTypes.TryGetValue(type, out var kept))
                    {
                        types.Add(new LuceneIndexContentType(kept.ContentTypeName, kept.ContentTypeDisplayName, 0));
                    }
                    else
                    {
                        errors.Add($"The content type {type} does not exist.");
                    }
                }

                int? identifier = path.Identifier is int id && storedPaths.Exists(p => p.Identifier == id) ? id : null;
                included.Add(new LuceneIndexIncludedPath(path.AliasPath.Trim()) { ContentTypes = types, Identifier = identifier });
            }

            if (errors.Count > 0)
            {
                return errors;
            }

            var updated = new LuceneIndexChannelConfiguration(channel.ChannelName, channel.ChannelDisplayName, included);
            int position = existing is null ? channels.Count : channels.IndexOf(existing);
            if (existing is not null)
            {
                channels.Remove(existing);
            }

            channels.Insert(position, updated);

            errors.AddRange(await GetChannelPermissionErrors(model.Channels, channels));
            model.Channels = channels;

            return errors;
        });


    [PageCommand(Permission = SystemPermissions.UPDATE)]
    public Task<ICommandResponse<IndexConfigurationSaveResult>> RemoveChannel(RemoveChannelArguments arguments) =>
        Save(async model =>
        {
            var channels = model.Channels.ToList();
            int removed = channels.RemoveAll(c => string.Equals(c.WebsiteChannelName, arguments.WebsiteChannelName, StringComparison.OrdinalIgnoreCase));
            if (removed == 0)
            {
                return ["The website channel is no longer part of the index. Reload the page."];
            }

            var errors = (await GetChannelPermissionErrors(model.Channels, channels)).ToList();
            model.Channels = channels;

            return errors;
        });


    internal static IEnumerable<string> ValidateChannel(string channelName, IReadOnlyList<ChannelSectionPath> paths)
    {
        if (paths.Count == 0)
        {
            yield return $"Add at least one path to {channelName}, or remove the channel.";
            yield break;
        }

        if (paths.Any(p => string.IsNullOrWhiteSpace(p.AliasPath)))
        {
            yield return $"Enter every path for {channelName}.";
        }

        if (paths.Any(p => !string.IsNullOrWhiteSpace(p.AliasPath) && !p.AliasPath.Trim().StartsWith('/')))
        {
            yield return "Paths must start with '/', for example /Articles/%.";
        }

        foreach (string duplicate in paths
            .Where(p => !string.IsNullOrWhiteSpace(p.AliasPath))
            .GroupBy(p => p.AliasPath.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key))
        {
            yield return $"The path {duplicate} is included more than once for {channelName}.";
        }

        foreach (var path in paths.Where(p => !string.IsNullOrWhiteSpace(p.AliasPath) && (p.ContentTypes ?? []).Count == 0))
        {
            yield return $"Select at least one content type for the path {path.AliasPath.Trim()}.";
        }
    }


    private async Task<ICommandResponse<IndexConfigurationSaveResult>> Save(Func<LuceneIndexModel, Task<IEnumerable<string>>> apply)
    {
        var stored = await storageService.GetIndexDataOrNullAsync(IndexIdentifier);
        if (stored is null)
        {
            return ResponseFrom(new IndexConfigurationSaveResult(false, ["The index no longer exists."], null));
        }

        // Sections change only their own values; the code name is never submitted.
        var model = new LuceneIndexModel
        {
            Id = stored.Id,
            IndexName = stored.IndexName,
            DisplayName = stored.DisplayName,
            StrategyName = ResolveOption(stored.StrategyName, StrategyStorage.Strategies.Keys),
            AnalyzerName = ResolveOption(stored.AnalyzerName, AnalyzerStorage.Analyzers.Keys),
            RebuildHook = stored.RebuildHook,
            LanguageNames = stored.LanguageNames.ToList(),
            ReusableContentTypeNames = stored.ReusableContentTypeNames.ToList(),
            Channels = stored.Channels.ToList(),
        };

        var errors = (await apply(model)).ToList();
        if (errors.Count > 0)
        {
            return ResponseFrom(new IndexConfigurationSaveResult(false, errors, null));
        }

        if (!await storageService.TryEditIndexAsync(model))
        {
            return ResponseFrom(new IndexConfigurationSaveResult(false, ["Could not update the index."], null));
        }

        var saved = await storageService.GetIndexDataOrNullAsync(IndexIdentifier) ?? model;

        return ResponseFrom(new IndexConfigurationSaveResult(true, [], ToData(saved, await GetContentTypes())))
            .AddSuccessMessage(SavedMessage);
    }


    private static IndexConfigurationData ToData(LuceneIndexModel model, IEnumerable<DataClassInfo> classes)
    {
        var names = classes
            .GroupBy(c => c.ClassName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().ClassDisplayName, StringComparer.OrdinalIgnoreCase);

        string Label(string name, string? fallback)
        {
            if (names.TryGetValue(name, out string? label))
            {
                return label;
            }

            return string.IsNullOrWhiteSpace(fallback) ? name : fallback;
        }

        return new IndexConfigurationData(
            model.Id,
            model.IndexName,
            model.DisplayName,
            ResolveOption(model.StrategyName, StrategyStorage.Strategies.Keys),
            ResolveOption(model.AnalyzerName, AnalyzerStorage.Analyzers.Keys),
            model.RebuildHook ?? "",
            model.LanguageNames.ToList(),
            model.ReusableContentTypeNames.Select(t => new WizardOption(t, Label(t, null))).ToList(),
            model.Channels
                .Select(c => new IndexConfigurationChannel(
                    c.WebsiteChannelName,
                    string.IsNullOrWhiteSpace(c.ChannelDisplayName) ? c.WebsiteChannelName : c.ChannelDisplayName,
                    (c.IncludedPaths ?? [])
                        .Select(p => new IndexConfigurationPath(
                            p.Identifier,
                            p.AliasPath,
                            (p.ContentTypes ?? []).Select(t => new WizardOption(t.ContentTypeName, Label(t.ContentTypeName, t.ContentTypeDisplayName))).ToList()))
                        .ToList()))
                .ToList());
    }


    // A blank stored option is shown and saved as the default the index uses.
    private static string ResolveOption(string stored, IEnumerable<string> options)
    {
        if (!string.IsNullOrWhiteSpace(stored))
        {
            return stored;
        }

        var all = options.ToList();

        return all.Find(o => o.Contains("Standard", StringComparison.OrdinalIgnoreCase)) ?? all.FirstOrDefault() ?? "";
    }


    private static async Task<List<DataClassInfo>> GetContentTypes() =>
        (await DataClassInfoProvider.ProviderObject.Get()
            .WhereIn(nameof(DataClassInfo.ClassContentTypeType), new[] { "Website", "Reusable" })
            .GetEnumerableTypedResultAsync(cancellationToken: CancellationToken.None))
            .ToList();


    private bool Allowed(ChannelInfo channel, AdminApplicationUser user, string permission) =>
        permissionEvaluator.Evaluate(new ApplicationPermissionEvaluationContext
        {
            ApplicationName = WebPagesApplicationPrefix + channel.ChannelGUID,
            User = user,
            PermissionName = permission
        });


    private async Task<IEnumerable<string>> GetChannelPermissionErrors(
        IEnumerable<LuceneIndexChannelConfiguration> stored,
        IEnumerable<LuceneIndexChannelConfiguration> submitted)
    {
        var before = stored.ToDictionary(c => c.WebsiteChannelName, StringComparer.OrdinalIgnoreCase);
        var after = submitted.ToDictionary(c => c.WebsiteChannelName, StringComparer.OrdinalIgnoreCase);

        var required = new List<(string Channel, string Permission)>();
        foreach (var (name, channel) in after)
        {
            if (!before.TryGetValue(name, out var previous) || Signature(previous) != Signature(channel))
            {
                required.Add((name, SystemPermissions.CREATE));
            }
        }

        required.AddRange(before.Keys.Where(name => !after.ContainsKey(name)).Select(name => (name, SystemPermissions.DELETE)));

        if (required.Count == 0)
        {
            return [];
        }

        var user = await userAccessor.Get();
        var names = required.Select(r => r.Channel).Distinct().ToList();
        var channels = (await channelProvider.Get()
            .WhereIn(nameof(ChannelInfo.ChannelName), names)
            .GetEnumerableTypedResultAsync())
            .ToDictionary(c => c.ChannelName, StringComparer.OrdinalIgnoreCase);

        var errors = new List<string>();
        foreach (var (name, permission) in required)
        {
            if (!channels.TryGetValue(name, out var channel))
            {
                // A deleted channel can always be removed from the index.
                if (permission != SystemPermissions.DELETE)
                {
                    errors.Add($"The website channel {name} does not exist.");
                }

                continue;
            }

            if (!Allowed(channel, user, permission))
            {
                eventLogService.LogError(nameof(IndexEditPage), nameof(GetChannelPermissionErrors),
                    $"A user with id: {user.UserID} tried to change the configuration of channel {name} for index {IndexIdentifier}, but does not have {permission} permission.");
                errors.Add($"You do not have the {permission} permission for the website channel {channel.ChannelDisplayName}.");
            }
        }

        return errors;
    }

    private static string Signature(LuceneIndexChannelConfiguration channel) =>
        string.Join("|", (channel.IncludedPaths ?? [])
            .Select(p => (p.AliasPath ?? "").Trim().ToLowerInvariant() + ":" +
                string.Join(",", (p.ContentTypes ?? []).Select(t => t.ContentTypeName.ToLowerInvariant()).Order()))
            .Order());
}
