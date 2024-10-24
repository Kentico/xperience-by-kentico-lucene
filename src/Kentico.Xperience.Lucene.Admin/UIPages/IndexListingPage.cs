using CMS.Core;
using CMS.Membership;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Lucene.Admin;
using Kentico.Xperience.Lucene.Core;
using Kentico.Xperience.Lucene.Core.Indexing;
using Kentico.Xperience.Lucene.Core.Scaling;

[assembly: UIPage(
   parentType: typeof(LuceneApplicationPage),
   slug: "indexes",
   uiPageType: typeof(IndexListingPage),
   name: "List of registered Lucene indices",
   templateName: TemplateNames.LISTING,
   order: UIPageOrder.NoOrder)]

namespace Kentico.Xperience.Lucene.Admin;

/// <summary>
/// An admin UI page that displays statistics about the registered Lucene indexes.
/// </summary>
[UIEvaluatePermission(SystemPermissions.VIEW)]
internal class IndexListingPage : ListingPage
{
    private readonly ILuceneClient luceneClient;
    private readonly IPageLinkGenerator pageLinkGenerator;
    private readonly ILuceneConfigurationStorageService configurationStorageService;
    private readonly IConversionService conversionService;
    private readonly ILuceneIndexManager indexManager;
    private readonly IWebFarmService webFarmService;

    protected override string ObjectType => LuceneIndexItemInfo.OBJECT_TYPE;

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexListingPage"/> class.
    /// </summary>
    public IndexListingPage(
        ILuceneClient luceneClient,
        IPageLinkGenerator pageLinkGenerator,
        ILuceneConfigurationStorageService configurationStorageService,
        ILuceneIndexManager indexManager,
        IConversionService conversionService,
        IWebFarmService webFarmService)
    {
        this.luceneClient = luceneClient;
        this.pageLinkGenerator = pageLinkGenerator;
        this.configurationStorageService = configurationStorageService;
        this.conversionService = conversionService;
        this.indexManager = indexManager;
        this.webFarmService = webFarmService;
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
                    Headline = "No indexes",
                    Content = "No Lucene indexes registered. See <a target='_blank' href='https://github.com/Kentico/kentico-xperience-lucene'>our instructions</a> to read more about creating and registering Lucene indexes.",
                    ContentAsHtml = true,
                    Type = CalloutType.FriendlyWarning,
                    Placement = CalloutPlacement.OnDesk
                }
            ];
        }

        PageConfiguration.ColumnConfigurations
            .AddColumn(nameof(LuceneIndexItemInfo.LuceneIndexItemId), "ID", defaultSortDirection: SortTypeEnum.Asc, sortable: true)
            .AddColumn(nameof(LuceneIndexItemInfo.LuceneIndexItemIndexName), "Name", sortable: true, searchable: true)
            .AddColumn(nameof(LuceneIndexItemInfo.LuceneIndexItemChannelName), "Channel", searchable: true, sortable: true)
            .AddColumn(nameof(LuceneIndexItemInfo.LuceneIndexItemStrategyName), "Index Strategy", searchable: true, sortable: true)
            .AddColumn(nameof(LuceneIndexItemInfo.LuceneIndexItemAnalyzerName), "Lucene Analyzer", searchable: true, sortable: true)
            // Placeholder field which will be replaced with a customized value
            .AddColumn(nameof(LuceneIndexItemInfo.LuceneIndexItemId), "Entries", sortable: true)
            .AddColumn(nameof(LuceneIndexItemInfo.LuceneIndexItemId), "Last Updated", sortable: true);

        PageConfiguration.AddEditRowAction<IndexEditPage>();
        PageConfiguration.TableActions.AddCommand("Rebuild", nameof(Rebuild), icon: Icons.RotateRight);
        PageConfiguration.TableActions.AddDeleteAction(nameof(Delete), "Delete");
        PageConfiguration.HeaderActions.AddLink<IndexCreatePage>("Create");

        await base.ConfigurePage();
    }

    protected override async Task<LoadDataResult> LoadData(LoadDataSettings settings, CancellationToken cancellationToken)
    {
        var result = await base.LoadData(settings, cancellationToken);

        var statistics = await luceneClient.GetStatistics(default);
        // Add statistics for indexes that are registered but not created in Lucene
        AddMissingStatistics(ref statistics, indexManager);

        if (PageConfiguration.ColumnConfigurations is not List<ColumnConfiguration> columns)
        {
            return result;
        }

        int entriesColIndex = columns.FindIndex(c => c.Caption == "Entries");
        int updatedColIndex = columns.FindIndex(c => c.Caption == "Last Updated");

        foreach (var row in result.Rows)
        {
            if (row.Cells is not List<Cell> cells)
            {
                continue;
            }

            var stats = GetStatistic(row, statistics);

            if (stats is null)
            {
                continue;
            }

            if (cells[entriesColIndex] is StringCell entriesCell)
            {
                entriesCell.Value = stats.Entries.ToString();
            }
            if (cells[updatedColIndex] is StringCell updatedCell)
            {
                updatedCell.Value = stats.UpdatedAt.ToLocalTime().ToString();
            }
        }

        return result;
    }

    private LuceneIndexStatisticsModel? GetStatistic(Row row, ICollection<LuceneIndexStatisticsModel> statistics)
    {
        int indexID = conversionService.GetInteger(row.Identifier, 0);
        string indexName = indexManager.GetIndex(indexID) is LuceneIndex index
            ? index.IndexName
            : "";

        return statistics.FirstOrDefault(s => string.Equals(s.Name, indexName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// A page command which rebuilds an Lucene index. Runs the rebuild on all application instances in case of vertical scaling.
    /// </summary>
    /// <param name="id">The ID of the row whose action was performed, which corresponds with the internal
    /// <see cref="LuceneIndex.Identifier"/> to rebuild.</param>
    /// <param name="cancellationToken">The cancellation token for the action.</param>
    [PageCommand(Permission = LuceneIndexPermissions.REBUILD)]
    public async Task<ICommandResponse<RowActionResult>> Rebuild(int id, CancellationToken cancellationToken)
    {
        var result = new RowActionResult(false);
        var index = indexManager.GetIndex(id);
        if (index is null)
        {
            return ResponseFrom(result)
                .AddErrorMessage(string.Format("Error loading Lucene index with identifier {0}.", id));
        }
        try
        {
            webFarmService.CreateTask(new RebuildWebFarmTask()
            {
                IndexName = index.IndexName,
                CreatorName = webFarmService.ServerName
            });

            await luceneClient.Rebuild(index.IndexName, cancellationToken);

            return ResponseFrom(result)
                .AddSuccessMessage("Indexing in progress. Visit your Lucene dashboard for details about the indexing process.");
        }
        catch (Exception ex)
        {
            EventLogService.LogException(nameof(IndexListingPage), nameof(Rebuild), ex);
            return ResponseFrom(result)
               .AddErrorMessage(string.Format("Errors occurred while rebuilding the '{0}' index. Please check the Event Log for more details.", index.IndexName));
        }
    }

    [PageCommand(Permission = SystemPermissions.DELETE)]
    public async Task<ICommandResponse> Delete(int id, CancellationToken _)
    {
        var index = indexManager.GetIndex(id);
        var result = new RowActionResult(false);

        if (index is null)
        {
            return await Task.FromResult<ICommandResponse>(ResponseFrom(result)
                .AddErrorMessage(string.Format("Error loading Lucene index with identifier {0}.", id)));
        }

        bool indexDeleted = await luceneClient.DeleteIndex(index!);

        if (!indexDeleted)
        {
            return await Task.FromResult<ICommandResponse>(ResponseFrom(result)
               .AddErrorMessage(string.Format("Errors occurred while deleting the '{0}' index. Please check the Event Log for more details.", index.IndexName)));
        }

        configurationStorageService.TryDeleteIndex(id);

        var response = NavigateTo(pageLinkGenerator.GetPath<IndexListingPage>());

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
