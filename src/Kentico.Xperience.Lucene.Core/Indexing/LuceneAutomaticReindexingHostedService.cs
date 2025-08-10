using System.Reflection;

using CMS.Core;
using CMS.DataEngine;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Kentico.Xperience.Lucene.Core.Indexing;

internal sealed class LuceneAutomaticReindexingHostedService : IHostedService
{
    private readonly IInfoProvider<LuceneIndexAssemblyVersionItemInfo> luceneAssemblyVersionInfoProvider;
    private readonly ILuceneClient luceneClient;
    private readonly AutomaticReindexingOptions? reindexingOptions;
    private readonly ILuceneIndexManager luceneIndexManager;
    private readonly IEventLogService eventLogService;
    private Task? backgroundTask;
    private readonly TimeSpan assemblyVersionCheckInterval;
    private const int MinimumCheckIntervalMinutes = 1;

    public LuceneAutomaticReindexingHostedService(
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

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (reindexingOptions is null || !reindexingOptions.Enabled || reindexingOptions.CheckIntervalMinutes < MinimumCheckIntervalMinutes)
        {
            return Task.CompletedTask;
        }

        eventLogService.LogInformation(
            "Kentico.Xperience.Lucene",
            $"{nameof(LuceneAutomaticReindexingHostedService)}.{nameof(StartAsync)}",
            "Lucene automatic reindexing if assembly version differs check started.");

        backgroundTask = Task.Run(() => RebuildIndexesIfAssemblyVersionsDiffer(cancellationToken), cancellationToken);

        return Task.CompletedTask;
    }

    private async Task RebuildIndexesIfAssemblyVersionsDiffer(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
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
                    $"{nameof(LuceneAutomaticReindexingHostedService)}.{nameof(RebuildIndexesIfAssemblyVersionsDiffer)}",
                    ex.Message);
            }

            await Task.Delay(assemblyVersionCheckInterval, cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        eventLogService.LogInformation(
            "Kentico.Xperience.Lucene",
            $"{nameof(LuceneAutomaticReindexingHostedService)}.{nameof(StopAsync)}",
            "Lucene automatic reindexing if assembly version differ check stopped.");

        if (backgroundTask is not null)
        {
            return Task.WhenAny(backgroundTask, Task.Delay(Timeout.Infinite, cancellationToken));
        }

        return Task.CompletedTask;
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
