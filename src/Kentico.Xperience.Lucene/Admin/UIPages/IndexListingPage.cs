using CMS.Core;
using CMS.Membership;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Lucene.Admin;
using Kentico.Xperience.Lucene.Indexing;
using Action = Kentico.Xperience.Admin.Base.Action;

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
internal class IndexListingPage : ListingPageBase<ListingConfiguration>
{
    private readonly ILuceneClient luceneClient;
    private readonly IPageUrlGenerator pageUrlGenerator;
    private readonly ILuceneConfigurationStorageService configurationStorageService;
    private readonly IUIPermissionEvaluator permissionEvaluator;
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
    public IndexListingPage(
        ILuceneClient luceneClient,
        IPageUrlGenerator pageUrlGenerator,
        ILuceneConfigurationStorageService configurationStorageService,
        IUIPermissionEvaluator permissionEvaluator)
    {
        this.luceneClient = luceneClient;
        this.pageUrlGenerator = pageUrlGenerator;
        this.configurationStorageService = configurationStorageService;
        this.permissionEvaluator = permissionEvaluator;
    }

    /// <inheritdoc/>
    public override async Task ConfigurePage()
    {
        if (!LuceneIndexStore.Instance.GetAllIndices().Any())
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

        PageConfiguration.ColumnConfigurations
            .AddColumn(nameof(LuceneIndexStatisticsViewModel.Name), "Name", defaultSortDirection: SortTypeEnum.Asc, searchable: true)
            .AddColumn(nameof(LuceneIndexStatisticsViewModel.Entries), LocalizationService.GetString("Indexed items"));

        var permissions = await GetUIPermissions();

        if (permissions.Rebuild)
        {
            PageConfiguration.TableActions.AddCommand(LocalizationService.GetString("Build index"), nameof(Rebuild), Icons.RotateRight);
        }
        if (permissions.Update)
        {
            PageConfiguration.TableActions.AddCommand("Edit", nameof(Edit));
        }
        if (permissions.Delete)
        {
            PageConfiguration.TableActions.AddCommand("Delete", nameof(Delete));
        }
        if (permissions.Create)
        {
            PageConfiguration.HeaderActions.AddLink<IndexCreatePage>("Create");
        }

        await base.ConfigurePage();
    }

    /// <inheritdoc/>
    protected override async Task<LoadDataResult> LoadData(LoadDataSettings settings, CancellationToken cancellationToken)
    {
        var permissions = await GetUIPermissions();

        try
        {
            var statistics = await luceneClient.GetStatistics(cancellationToken);

            // Add statistics for indexes that are registered but not created in Lucene
            AddMissingStatistics(ref statistics);

            // Remove statistics for indexes that are not registered in this instance
            var filteredStatistics = statistics.Where(stat =>
                LuceneIndexStore.Instance.GetAllIndices().Any(index => index.IndexName.Equals(stat.Name, StringComparison.OrdinalIgnoreCase)));

            var searchedStatistics = DoSearch(filteredStatistics, settings.SearchTerm);
            var orderedStatistics = SortStatistics(searchedStatistics, settings);
            var rows = orderedStatistics.Select(stat => GetRow(stat, permissions));

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

    private Row GetRow(LuceneIndexStatisticsViewModel statistics, UIPermissions permissions)
    {
        if (statistics.Name is null || LuceneIndexStore.Instance.GetIndex(statistics.Name) is not LuceneIndex index)
        {
            throw new InvalidOperationException($"Unable to retrieve Lucene index with name '{statistics.Name}.'");
        }

        var row = new Row
        {
            Identifier = index.Identifier,
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
            }
        };

        var actions = new List<Action>();

        if (permissions.Update)
        {
            row.Action = new Action(ActionType.Command)
            {
                Parameter = nameof(RowClick)
            };
            actions.Add(new(ActionType.Command)
            {
                Title = "Edit",
                Label = "Edit",
                Parameter = nameof(Edit),
                Icon = Icons.Edit
            });
        }
        if (permissions.Rebuild)
        {
            string label = statistics.Entries == 0
                ? "Build index"
                : "Re-build index";

            actions.Add(new(ActionType.Command)
            {
                Title = label,
                Label = label,
                Icon = Icons.RotateRight,
                Parameter = nameof(Rebuild)
            });
        }
        if (permissions.Delete)
        {
            actions.Add(new(ActionType.Command)
            {
                Title = "Delete",
                Label = "Delete",
                Parameter = nameof(Delete),
                Icon = Icons.Bin
            });
        }
        row.Cells.Add(new ActionCell
        {
            Actions = actions
        });

        return row;
    }

    /// <summary>
    /// A page command which displays details about an index.
    /// </summary>
    /// <param name="id">The ID of the row that was clicked, which corresponds with the internal
    /// <see cref="LuceneIndex.Identifier"/> to display.</param>
    [PageCommand(Permission = SystemPermissions.UPDATE)]
    public async Task<INavigateResponse> RowClick(int id) =>
        await Task.FromResult(NavigateTo(pageUrlGenerator.GenerateUrl<IndexEditPage>(id.ToString())));

    /// <summary>
    /// A page command which rebuilds an Lucene index.
    /// </summary>
    /// <param name="id">The ID of the row whose action was performed, which corresponds with the internal
    /// <see cref="LuceneIndex.Identifier"/> to rebuild.</param>
    /// <param name="cancellationToken">The cancellation token for the action.</param>
    [PageCommand(Permission = LuceneIndexPermissions.REBUILD)]
    public async Task<ICommandResponse<RowActionResult>> Rebuild(int id, CancellationToken cancellationToken)
    {
        var result = new RowActionResult(false);
        var index = LuceneIndexStore.Instance.GetIndex(id);
        if (index is null)
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

    [PageCommand(Permission = SystemPermissions.UPDATE)]
    public Task<INavigateResponse> Edit(int id, CancellationToken _) =>
        Task.FromResult(NavigateTo(pageUrlGenerator.GenerateUrl<IndexEditPage>(id.ToString())));

    [PageCommand(Permission = SystemPermissions.DELETE)]
    public Task<ICommandResponse> Delete(int id, CancellationToken _)
    {
        bool res = configurationStorageService.TryDeleteIndex(id);
        if (res)
        {
            var indices = configurationStorageService.GetAllIndexData();

            LuceneIndexStore.Instance.SetIndicies(indices);
        }
        var response = NavigateTo(pageUrlGenerator.GenerateUrl<IndexListingPage>());

        return Task.FromResult<ICommandResponse>(response);
    }

    private static void AddMissingStatistics(ref ICollection<LuceneIndexStatisticsViewModel> statistics)
    {
        foreach (string indexName in LuceneIndexStore.Instance.GetAllIndices().Select(i => i.IndexName))
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

    private static IEnumerable<LuceneIndexStatisticsViewModel> SortStatistics(IEnumerable<LuceneIndexStatisticsViewModel> statistics, LoadDataSettings settings)
    {
        if (string.IsNullOrEmpty(settings.SortBy))
        {
            return statistics;
        }

        return settings.SortType == SortTypeEnum.Desc
            ? statistics.OrderByDescending(stat => stat.GetType().GetProperty(settings.SortBy)?.GetValue(stat, null))
            : statistics.OrderBy(stat => stat.GetType().GetProperty(settings.SortBy)?.GetValue(stat, null));
    }

    private async Task<UIPermissions> GetUIPermissions()
    {
        var permissions = new UIPermissions
        {
            Create = (await permissionEvaluator.Evaluate(SystemPermissions.CREATE)).Succeeded,
            Delete = (await permissionEvaluator.Evaluate(SystemPermissions.DELETE)).Succeeded,
            Update = (await permissionEvaluator.Evaluate(SystemPermissions.UPDATE)).Succeeded,
            Rebuild = (await permissionEvaluator.Evaluate(LuceneIndexPermissions.REBUILD)).Succeeded,
            View = (await permissionEvaluator.Evaluate(SystemPermissions.VIEW)).Succeeded
        };

        return permissions;
    }
}

internal record struct UIPermissions
{
    public bool View { get; set; }
    public bool Update { get; set; }
    public bool Delete { get; set; }
    public bool Rebuild { get; set; }
    public bool Create { get; set; }
}
