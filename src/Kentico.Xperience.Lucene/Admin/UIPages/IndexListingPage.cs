﻿using CMS.Core;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Lucene.Admin;
using Kentico.Xperience.Lucene.Models;
using Kentico.Xperience.Lucene.Services;
using Action = Kentico.Xperience.Admin.Base.Action;

[assembly: UIPage(
   parentType: typeof(LuceneApplication),
   slug: "indexes",
   uiPageType: typeof(IndexListingPage),
   name: "List of registered Lucene indices",
   templateName: TemplateNames.LISTING,
   order: UIPageOrder.First)]

namespace Kentico.Xperience.Lucene.Admin;

/// <summary>
/// An admin UI page that displays statistics about the registered Lucene indexes.
/// </summary>
internal class IndexListingPage : ListingPageBase<ListingConfiguration>
{
    private readonly ILuceneClient luceneClient;
    private readonly IPageUrlGenerator pageUrlGenerator;
    private readonly IConfigurationStorageService configurationStorageService;
    private ListingConfiguration? mPageConfiguration;

    /// <inheritdoc/>
    public override ListingConfiguration PageConfiguration
    {
        get
        {
            mPageConfiguration ??= new ListingConfiguration()
            {
                Caption = LocalizationService.GetString("List of indices"),
                ColumnConfigurations = new List<ColumnConfiguration>(),
                TableActions = new List<ActionConfiguration>(),
                HeaderActions = new List<ActionConfiguration>(),
                PageSizes = new List<int> { 10, 25 }
            };

            return mPageConfiguration;

        }
        set => mPageConfiguration = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexListingPage"/> class.
    /// </summary>
    public IndexListingPage(ILuceneClient luceneClient, IPageUrlGenerator pageUrlGenerator, IConfigurationStorageService configurationStorageService)
    {
        this.luceneClient = luceneClient;
        this.pageUrlGenerator = pageUrlGenerator;
        this.configurationStorageService = configurationStorageService;
    }

    /// <inheritdoc/>
    public override Task ConfigurePage()
    {
        if (!IndexStore.Instance.GetAllIndices().Any())
        {
            PageConfiguration.Callouts =
            new()
            {
                new()
                {
                    Headline = "No indexes",
                    Content = "No Lucene indexes registered. See <a target='_blank' href='https://github.com/Kentico/kentico-xperience-lucene'>our instructions</a> to read more about creating and registering Lucene indexes.",
                    ContentAsHtml = true,
                    Type = CalloutType.FriendlyWarning,
                    Placement = CalloutPlacement.OnDesk
                }
            };
        }

        PageConfiguration.HeaderActions.AddLink<IndexEditPage>("Create", parameters: "-1");

        PageConfiguration.ColumnConfigurations
            .AddColumn(nameof(LuceneIndexStatisticsViewModel.Name), "Name", defaultSortDirection: SortTypeEnum.Asc, searchable: true)
            .AddColumn(nameof(LuceneIndexStatisticsViewModel.Entries), LocalizationService.GetString("Indexed items"));

        PageConfiguration.TableActions.AddCommand(LocalizationService.GetString("Build index"), nameof(Rebuild), Icons.RotateRight);
        PageConfiguration.TableActions.AddCommand("Edit", nameof(Edit));
        PageConfiguration.TableActions.AddCommand("Delete", nameof(Delete));
        return base.ConfigurePage();
    }

    /// <summary>
    /// A page command which displays details about an index.
    /// </summary>
    /// <param name="id">The ID of the row that was clicked, which corresponds with the internal
    /// <see cref="LuceneIndex.Identifier"/> to display.</param>
    [PageCommand]
    public async Task<INavigateResponse> RowClick(int id)
        => await Task.FromResult(NavigateTo(pageUrlGenerator.GenerateUrl<IndexEditPage>(id.ToString())));

    /// <summary>
    /// A page command which rebuilds an Lucene index.
    /// </summary>
    /// <param name="id">The ID of the row whose action was performed, which corresponds with the internal
    /// <see cref="LuceneIndex.Identifier"/> to rebuild.</param>
    /// <param name="cancellationToken">The cancellation token for the action.</param>
    [PageCommand]
    public async Task<ICommandResponse<RowActionResult>> Rebuild(int id, CancellationToken cancellationToken)
    {
        var result = new RowActionResult(false);
        var index = IndexStore.Instance.GetIndex(id);
        if (index == null)
        {
            return ResponseFrom(result)
                .AddErrorMessage(string.Format("Error loading Lucene index with identifier {0}.", id));
        }
        try
        {
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

    [PageCommand]
    public Task<INavigateResponse> Edit(int id, CancellationToken _) => Task.FromResult(NavigateTo(pageUrlGenerator.GenerateUrl<IndexEditPage>(id.ToString())));

    [PageCommand]
    public Task<ICommandResponse> Delete(int id, CancellationToken _)
    {
        bool res = configurationStorageService.TryDeleteIndex(id);
        if (res)
        {
            var indices = configurationStorageService.GetAllIndexData();

            IndexStore.Instance.AddIndices(indices);
        }
        var response = NavigateTo(pageUrlGenerator.GenerateUrl<IndexListingPage>());

        return Task.FromResult<ICommandResponse>(response);
    }

    /// <inheritdoc/>
    protected override async Task<LoadDataResult> LoadData(LoadDataSettings settings, CancellationToken cancellationToken)
    {
        try
        {
            var statistics = await luceneClient.GetStatistics(cancellationToken);

            // Add statistics for indexes that are registered but not created in Lucene
            AddMissingStatistics(ref statistics);

            // Remove statistics for indexes that are not registered in this instance
            var filteredStatistics = statistics.Where(stat =>
                IndexStore.Instance.GetAllIndices().Any(index => index.IndexName.Equals(stat.Name, StringComparison.OrdinalIgnoreCase)));

            var searchedStatistics = DoSearch(filteredStatistics, settings.SearchTerm);
            var orderedStatistics = SortStatistics(searchedStatistics, settings);
            var rows = orderedStatistics.Select(stat => GetRow(stat));

            return new LoadDataResult
            {
                Rows = rows,
                TotalCount = rows.Count()
            };
        }
        catch (Exception ex)
        {
            EventLogService.LogException(nameof(IndexListingPage), nameof(LoadData), ex);
            return new LoadDataResult
            {
                Rows = Enumerable.Empty<Row>(),
                TotalCount = 0
            };
        }
    }


    private static void AddMissingStatistics(ref ICollection<LuceneIndexStatisticsViewModel> statistics)
    {
        foreach (string indexName in IndexStore.Instance.GetAllIndices().Select(i => i.IndexName))
        {
            if (!statistics.Any(stat => stat.Name?.Equals(indexName, StringComparison.OrdinalIgnoreCase) ?? false))
            {
                statistics.Add(new LuceneIndexStatisticsViewModel
                {
                    Name = indexName,
                    Entries = 0,
                    UpdatedAt = DateTime.MinValue
                });
            }
        }
    }


    private static IEnumerable<LuceneIndexStatisticsViewModel> DoSearch(IEnumerable<LuceneIndexStatisticsViewModel> statistics, string searchTerm)
    {
        if (string.IsNullOrEmpty(searchTerm))
        {
            return statistics;
        }

        return statistics.Where(stat => stat.Name?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private Row GetRow(LuceneIndexStatisticsViewModel statistics)
    {
        var luceneIndex = statistics.Name != null ? IndexStore.Instance.GetIndex(statistics.Name) : null;
        return luceneIndex == null
            ? throw new InvalidOperationException($"Unable to retrieve Lucene index with name '{statistics.Name}.'")
            : new Row
            {
                Identifier = luceneIndex.Identifier,
                Action = new Action(ActionType.Command)
                {
                    Parameter = nameof(RowClick)
                },
                Cells = new List<Cell>
                {
                    new StringCell
                    {
                        Value = statistics.Name
                    },
                    new StringCell
                    {
                        Value = statistics.Entries.ToString()
                    },
                    new ActionCell
                    {
                        Actions = new List<Action>
                        {
                            new(ActionType.Command)
                            {
                                Title = "Build index",
                                Label = "Build index",
                                Icon = Icons.RotateRight,
                                Parameter = nameof(Rebuild)
                            },
                            new(ActionType.Command)
                            {
                                Title = "Edit",
                                Label = "Edit",
                                Parameter = nameof(Edit),
                                Icon = Icons.Edit
                            },
                            new(ActionType.Command)
                            {
                                Title = "Delete",
                                Label = "Delete",
                                Parameter = nameof(Delete),
                                Icon = Icons.Bin
                            }
                        }
                    }
                }
            };
    }

    private static IEnumerable<LuceneIndexStatisticsViewModel> SortStatistics(IEnumerable<LuceneIndexStatisticsViewModel> statistics, LoadDataSettings settings)
    {
        if (string.IsNullOrEmpty(settings.SortBy))
        {
            return statistics;
        }

        return settings.SortType == SortTypeEnum.Desc ?
         statistics.OrderByDescending(stat => stat.GetType().GetProperty(settings.SortBy)?.GetValue(stat, null))
         : statistics.OrderBy(stat => stat.GetType().GetProperty(settings.SortBy)?.GetValue(stat, null));
    }
}