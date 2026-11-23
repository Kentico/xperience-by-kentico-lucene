using CMS.Core;
using CMS.Membership;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Lucene.Admin;
using Kentico.Xperience.Lucene.Core;
using Kentico.Xperience.Lucene.Core.Indexing;

[assembly: UIPage(
   parentType: typeof(LuceneApplicationPage),
   slug: "indexes",
   uiPageType: typeof(IndexListingPage),
   name: "Indexes",
   templateName: TemplateNames.LISTING,
   order: UIPageOrder.NoOrder)]

namespace Kentico.Xperience.Lucene.Admin;

/// <summary>
/// An admin UI page that displays statistics about the registered Lucene indexes.
/// </summary>
[UIEvaluatePermission(SystemPermissions.VIEW)]
public class IndexListingPage : ListingPage
{
    private readonly ILuceneClient luceneClient;
    private readonly IPageLinkGenerator pageLinkGenerator;
    private readonly ILuceneConfigurationStorageService configurationStorageService;
    private readonly IConversionService conversionService;
    private readonly ILuceneIndexManager indexManager;


    private const string DISPLAY_NAME_CAPTION = "Display name";
    private const string DOCUMENTS_CAPTION = "Documents";
    private const string LAST_UPDATED_CAPTION = "Last updated";
    private const string UNAVAILABLE = "Unavailable";


    protected override string ObjectType => LuceneIndexItemInfo.OBJECT_TYPE;


    /// <summary>
    /// Initializes a new instance of the <see cref="IndexListingPage"/> class.
    /// </summary>
    public IndexListingPage(
        ILuceneClient luceneClient,
        IPageLinkGenerator pageLinkGenerator,
        ILuceneConfigurationStorageService configurationStorageService,
        ILuceneIndexManager indexManager,
        IConversionService conversionService)
    {
        this.luceneClient = luceneClient;
        this.pageLinkGenerator = pageLinkGenerator;
        this.configurationStorageService = configurationStorageService;
        this.conversionService = conversionService;
        this.indexManager = indexManager;
    }


    /// <inheritdoc/>
    public override async Task ConfigurePage()
    {
        if (!indexManager.GetAllIndices().Any())
        {
            PageConfiguration.Callouts =
            [
                new()
                {
                    Headline = "No indexes yet",
                    Content = "Select <strong>New index</strong> to create a Lucene index, or register one in code. See the <a target='_blank' rel='noopener noreferrer' href='https://github.com/Kentico/xperience-by-kentico-lucene'>Lucene integration documentation</a> for details.",
                    ContentAsHtml = true,
                    Type = CalloutType.FriendlyWarning,
                    Placement = CalloutPlacement.OnDesk
                }
            ];
        }

        // Documents and Last updated are filled from Lucene in LoadData, so they use the GUID column as a
        // placeholder (not the sorted ID column) and cannot be sorted or searched by the database query.
        PageConfiguration.ColumnConfigurations
            .AddColumn(nameof(LuceneIndexItemInfo.LuceneIndexItemId), "ID", defaultSortDirection: SortTypeEnum.Asc, sortable: true)
            .AddColumn(nameof(LuceneIndexItemInfo.LuceneIndexItemDisplayName), DISPLAY_NAME_CAPTION, sortable: true, searchable: true)
            .AddColumn(nameof(LuceneIndexItemInfo.LuceneIndexItemIndexName), "Code name", sortable: true, searchable: true)
            .AddColumn(nameof(LuceneIndexItemInfo.LuceneIndexItemStrategyName), "Indexing strategy", searchable: true, sortable: true)
            .AddColumn(nameof(LuceneIndexItemInfo.LuceneIndexItemAnalyzerName), "Analyzer", searchable: true, sortable: true)
            .AddColumn(nameof(LuceneIndexItemInfo.LuceneIndexItemGuid), DOCUMENTS_CAPTION, sortable: false,
                tooltip: "Number of documents stored in the index on this application instance.")
            .AddColumn(nameof(LuceneIndexItemInfo.LuceneIndexItemGuid), LAST_UPDATED_CAPTION, sortable: false,
                tooltip: "Last write to the index on this application instance, in server time. A rebuild runs in the background, so this can change after the rebuild request.");

        PageConfiguration.AddEditRowAction<IndexEditSection>();
        PageConfiguration.TableActions.AddCommand("Rebuild", nameof(Rebuild), icon: Icons.RotateRight, title: "Rebuild index");
        PageConfiguration.TableActions.AddDeleteAction(nameof(Delete), "Delete", title: "Delete index");
        if (PageConfiguration.TableActions.LastOrDefault()?.ConfirmationConfiguration is ConfirmationConfiguration deleteConfirmation)
        {
            deleteConfirmation.Title = "Delete index?";
            deleteConfirmation.Detail = "The index files and its configuration are removed. Searches that use this index stop returning results. This cannot be undone.";
        }
        PageConfiguration.HeaderActions.AddLink<IndexCreatePage>("New index");

        await base.ConfigurePage();
    }


    /// <inheritdoc/>
    protected override async Task<LoadDataResult> LoadData(LoadDataSettings settings, CancellationToken cancellationToken)
    {
        var result = await base.LoadData(settings, cancellationToken);

        ICollection<LuceneIndexStatisticsModel> statistics = [];
        bool statisticsAvailable = true;
        try
        {
            statistics = await luceneClient.GetStatistics(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            statisticsAvailable = false;
            EventLogService.LogException(nameof(IndexListingPage), nameof(LoadData), ex);
        }

        // Add statistics for indexes that are registered but not created in Lucene
        if (statisticsAvailable)
        {
            AddMissingStatistics(ref statistics, indexManager);
        }

        if (PageConfiguration.ColumnConfigurations is not List<ColumnConfiguration> columns)
        {
            return result;
        }

        int displayColIndex = columns.FindIndex(c => c.Caption == DISPLAY_NAME_CAPTION);
        int entriesColIndex = columns.FindIndex(c => c.Caption == DOCUMENTS_CAPTION);
        int updatedColIndex = columns.FindIndex(c => c.Caption == LAST_UPDATED_CAPTION);

        foreach (var row in result.Rows)
        {
            if (row.Cells is not List<Cell> cells)
            {
                continue;
            }

            var index = indexManager.GetIndex(conversionService.GetInteger(row.Identifier, 0));

            // Indexes created before display names existed have none stored; show the code name instead.
            if (displayColIndex >= 0 && cells[displayColIndex] is StringCell displayCell
                && string.IsNullOrWhiteSpace(displayCell.Value?.ToString())
                && index is not null)
            {
                displayCell.Value = index.DisplayName;
            }

            // The placeholder cells hold the row GUID until replaced, so never leave them unchanged.
            var stats = statisticsAvailable && index is not null
                ? statistics.FirstOrDefault(s => string.Equals(s.Name, index.IndexName, StringComparison.OrdinalIgnoreCase))
                : null;

            if (entriesColIndex >= 0 && cells[entriesColIndex] is StringCell entriesCell)
            {
                entriesCell.Value = stats?.Entries.ToString() ?? UNAVAILABLE;
            }
            if (updatedColIndex >= 0 && cells[updatedColIndex] is StringCell updatedCell)
            {
                updatedCell.Value = FormatLastUpdated(stats);
            }
        }

        return result;
    }


    private static string FormatLastUpdated(LuceneIndexStatisticsModel? stats)
    {
        if (stats is null)
        {
            return UNAVAILABLE;
        }

        return stats.UpdatedAt == DateTime.MinValue ? "Never" : stats.UpdatedAt.ToLocalTime().ToString();
    }


    /// <summary>
    /// A page command which rebuilds an Lucene index. Runs the rebuild on all application instances in case of vertical scaling.
    /// </summary>
    /// <param name="id">The ID of the row whose action was performed, which corresponds with the internal
    /// <see cref="LuceneIndex.Identifier"/> to rebuild.</param>
    /// <returns>The <see cref="ICommandResponse{RowActionResult}"/>.</returns>
    [PageCommand(Permission = LuceneIndexPermissions.REBUILD)]
    public async Task<ICommandResponse<RowActionResult>> Rebuild(int id)
    {
        var result = new RowActionResult(false);
        var index = indexManager.GetIndex(id);
        if (index is null)
        {
            return ResponseFrom(result)
                .AddErrorMessage(string.Format("Index with ID {0} is not loaded in this application. Refresh the listing; if the index is still listed, check the Event Log.", id));
        }
        try
        {
            // Rebuild should be allowed to finish even if the user navigates away from the page,
            // as it is a long-running operation and cancelling it prematurely may lead to an inconsistent state.
            await luceneClient.Rebuild(index.IndexName, CancellationToken.None);

            return ResponseFrom(result)
                .AddSuccessMessage("Rebuild requested. Items are indexed in the background and completion is not tracked. Open the index Overview tab to check the current state.");
        }
        catch (Exception ex)
        {
            EventLogService.LogException(nameof(IndexListingPage), nameof(Rebuild), ex);
            return ResponseFrom(result)
               .AddErrorMessage(string.Format("Errors occurred while rebuilding the '{0}' index. Please check the Event Log for more details.", index.IndexName));
        }
    }


    /// <summary>
    /// A page command which deletes a Lucene index. Runs the delete on all application instances in case of vertical scaling.
    /// </summary>
    /// <param name="id">The id of the index to delete.</param>
    /// <returns>The <see cref="ICommandResponse{RowActionResult}"/>.</returns>
    [PageCommand(Permission = SystemPermissions.DELETE)]
    public new async Task<ICommandResponse> Delete(int id)
    {
        var index = indexManager.GetIndex(id);
        var result = new RowActionResult(false);

        if (index is null)
        {
            return await Task.FromResult<ICommandResponse>(ResponseFrom(result)
                .AddErrorMessage(string.Format("Index with ID {0} is not loaded in this application. Refresh the listing; if the index is still listed, check the Event Log.", id)));
        }

        bool indexDeleted;
        try
        {
            indexDeleted = await luceneClient.DeleteIndex(index);
        }
        catch (Exception ex)
        {
            EventLogService.LogException(nameof(IndexListingPage), nameof(Delete), ex);
            indexDeleted = false;
        }

        if (!indexDeleted)
        {
            return await Task.FromResult<ICommandResponse>(ResponseFrom(result)
               .AddErrorMessage(string.Format("Errors occurred while deleting the '{0}' index. Please check the Event Log for more details.", index.IndexName)));
        }

        configurationStorageService.TryDeleteIndex(id);

        var response = NavigateTo(pageLinkGenerator.GetPath<IndexListingPage>())
            .AddSuccessMessage(string.Format("Index '{0}' deleted.", index.IndexName));

        return await Task.FromResult<ICommandResponse>(response);
    }


    private static void AddMissingStatistics(ref ICollection<LuceneIndexStatisticsModel> statistics, ILuceneIndexManager indexManager)
    {
        foreach (string indexName in indexManager.GetAllIndices().Select(i => i.IndexName))
        {
            if (!statistics.Any(stat => stat.Name?.Equals(indexName, StringComparison.OrdinalIgnoreCase) ?? false))
            {
                statistics.Add(new LuceneIndexStatisticsModel
                {
                    Name = indexName,
                    Entries = 0,
                    UpdatedAt = DateTime.MinValue
                });
            }
        }
    }
}
