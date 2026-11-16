#nullable enable

using CMS;
using CMS.Base;
using CMS.Core;
using CMS.DataEngine;

using DancingGoat.Search;

using Kentico.Xperience.Lucene.Core.Indexing;
using Kentico.Xperience.Lucene.Core.Search;

using Microsoft.Extensions.DependencyInjection;

[assembly: RegisterModule(typeof(DancingGoatSearchIndexRebuildModule))]

namespace DancingGoat.Search;

/// <summary>
/// Rebuilds the Dancing Goat search index on startup when it has no documents.
/// </summary>
/// <remarks>
/// The index configuration is restored from the CI repository, but the index files are not stored in source control.
/// Without this module, a freshly set up site would return no search results until the index is rebuilt in the administration.
/// </remarks>
internal sealed class DancingGoatSearchIndexRebuildModule : Module
{
    private const string EVENT_LOG_SOURCE = nameof(DancingGoatSearchIndexRebuildModule);

    public DancingGoatSearchIndexRebuildModule()
        : base(nameof(DancingGoatSearchIndexRebuildModule))
    {
    }

    protected override void OnInit(ModuleInitParameters parameters)
    {
        base.OnInit(parameters);

        var services = parameters.Services;

        // Rebuilding can take a while, so it runs in the background instead of delaying application start.
        // The execution context is not flowed so the background work does not share the startup database connection.
        ApplicationEvents.Initialized.Execute += (sender, eventArgs) =>
        {
            using (ExecutionContext.SuppressFlow())
            {
                _ = Task.Run(() => RebuildIfEmpty(services));
            }
        };
    }

    private static async Task RebuildIfEmpty(IServiceProvider services)
    {
        var eventLogService = services.GetRequiredService<IEventLogService>();

        try
        {
            var index = services.GetRequiredService<ILuceneIndexManager>().GetIndex(DancingGoatSearchIndexingStrategy.INDEX_NAME);
            if (index is null)
            {
                eventLogService.LogWarning(
                    EVENT_LOG_SOURCE,
                    "INDEX_NOT_FOUND",
                    $"The '{DancingGoatSearchIndexingStrategy.INDEX_NAME}' Lucene index is not configured. Restore the CI repository or create the index in the administration.");
                return;
            }

            var documentCount = services
                .GetRequiredService<ILuceneSearchService>()
                .UseSearcher(index, searcher => searcher.IndexReader.NumDocs);

            if (documentCount == 0)
            {
                await services
                    .GetRequiredService<ILuceneClient>()
                    .Rebuild(DancingGoatSearchIndexingStrategy.INDEX_NAME, CancellationToken.None);
            }
        }
        catch (Exception exception)
        {
            eventLogService.LogException(EVENT_LOG_SOURCE, "INDEX_REBUILD_FAILED", exception);
        }
    }
}
