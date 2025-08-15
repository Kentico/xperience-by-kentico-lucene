using System.Reflection;

using CMS.Core;
using CMS.DataEngine;

using Microsoft.Extensions.Options;

namespace Kentico.Xperience.Lucene.Core.Indexing;

/// <summary>
/// A background service that automatically reindexes Lucene indexes when the assembly version changes.
/// </summary>
/// <remarks>This service periodically checks if the assembly version of the application differs from the version
/// associated with the Lucene indexes. If a difference is detected, the affected indexes are rebuilt. The service is
/// configured using the <see cref="LuceneSearchOptions.PostStartupReindexingOptions"/> settings.</remarks>
internal sealed class LuceneAutomaticReindexingBackgroundService : ApplicationLifecycleBackgroundService
{
    private readonly IInfoProvider<LuceneIndexAssemblyVersionItemInfo> luceneAssemblyVersionInfoProvider;


    private readonly ILuceneClient luceneClient;


    private readonly AutomaticReindexingOptions? reindexingOptions;


    private readonly ILuceneIndexManager luceneIndexManager;


    private readonly IEventLogService eventLogService;


    private readonly TimeSpan assemblyVersionCheckInterval;


    private const int MinimumCheckIntervalMinutes = 1;


    public LuceneAutomaticReindexingBackgroundService(
        IInfoProvider<LuceneIndexAssemblyVersionItemInfo> luceneAssemblyVersionInfoProvider,
        ILuceneClient luceneClient,
        ILuceneIndexManager luceneIndexManager,
        IEventLogService eventLogService,
        IOptions<LuceneSearchOptions> luceneSearchOptions
    )
    {
        this.luceneIndexManager = luceneIndexManager;
        this.eventLogService = eventLogService;
        this.luceneAssemblyVersionInfoProvider = luceneAssemblyVersionInfoProvider;
        this.luceneClient = luceneClient;
        reindexingOptions = luceneSearchOptions.Value.PostStartupReindexingOptions;
        assemblyVersionCheckInterval = TimeSpan.FromMinutes(reindexingOptions?.CheckIntervalMinutes ?? 0);
    }


    /// <inheritdoc/>
    protected override async Task ExecuteInternal(CancellationToken stoppingToken)
    {
        if (reindexingOptions is null || !reindexingOptions.Enabled || reindexingOptions.CheckIntervalMinutes < MinimumCheckIntervalMinutes)
        {
            return;
        }

        eventLogService.LogInformation(
        "Kentico.Xperience.Lucene",
        $"{nameof(LuceneAutomaticReindexingBackgroundService)}.{nameof(StartAsync)}",
        "Lucene automatic reindexing if assembly version differs check started.");

        using var timer = new PeriodicTimer(assemblyVersionCheckInterval);

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RebuildIndexesIfAssemblyVersionsDiffer(stoppingToken);
        }
    }


    private async Task RebuildIndexesIfAssemblyVersionsDiffer(CancellationToken cancellationToken)
    {
        try
        {
            var assembly = GetAppBuildVersion();
            var luceneAssemblyVersionItems = await luceneAssemblyVersionInfoProvider.Get().GetEnumerableTypedResultAsync();

            var luceneIndexesToRebuild = luceneIndexManager
                .GetAllIndices()
                .ExceptBy(reindexingOptions!.IndexesExcludedFromAutomaticReindexing, x => x.IndexName);

            foreach (var index in luceneIndexesToRebuild)
            {
                var assemblyVersionDiffers = await SetIfLuceneIndexAssemblyVersionDiffersAsync(index, luceneAssemblyVersionItems, assembly, cancellationToken);
                if (assemblyVersionDiffers)
                {
                    await luceneClient.Rebuild(index.IndexName, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            eventLogService.LogError(
                "Kentico.Xperience.Lucene",
                $"{nameof(LuceneAutomaticReindexingBackgroundService)}.{nameof(RebuildIndexesIfAssemblyVersionsDiffer)}",
                ex.Message);
        }
    }


    private static string GetAppBuildVersion()
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        return assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? "1.0.0";
    }


    private async Task<bool> SetIfLuceneIndexAssemblyVersionDiffersAsync(LuceneIndex index, IEnumerable<LuceneIndexAssemblyVersionItemInfo> luceneAssemblyVersionItems, string assemblyName, CancellationToken cancellationToken)
    {
        var assemblyVersionItem = luceneAssemblyVersionItems?
            .FirstOrDefault(x => x.LuceneIndexAssemblyVersionItemIndexItemId == index.Identifier) ??
            new LuceneIndexAssemblyVersionItemInfo
            {
                LuceneIndexAssemblyVersionItemIndexItemId = index.Identifier
            };

        var assemblyVersionDiffers = !Equals(assemblyVersionItem.LuceneIndexAssemblyVersionItemAssemblyVersion, assemblyName);

        if (assemblyVersionDiffers)
        {
            assemblyVersionItem.LuceneIndexAssemblyVersionItemAssemblyVersion = assemblyName;
        }

        await luceneAssemblyVersionInfoProvider.SetAsync(assemblyVersionItem, cancellationToken);

        return assemblyVersionDiffers;
    }
}
