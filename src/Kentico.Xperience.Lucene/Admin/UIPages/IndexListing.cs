using CMS.Core;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Lucene.Models;
using Kentico.Xperience.Lucene.Services;

using Action = Kentico.Xperience.Admin.Base.Action;

namespace Kentico.Xperience.Lucene.Admin
{
    /// <summary>
    /// An admin UI page that displays statistics about the registered Lucene indexes.
    /// </summary>
    internal class IndexListing : ListingPageBase<ListingConfiguration>
    {
        private readonly ILuceneClient luceneClient;
        private readonly IPageUrlGenerator pageUrlGenerator;
        private ListingConfiguration? mPageConfiguration;


        /// <inheritdoc/>
        public override ListingConfiguration PageConfiguration
        {
            get
            {
                mPageConfiguration ??= new ListingConfiguration()
                {
                    Caption = LocalizationService.GetString("integrations.lucene.listing.caption"),
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
        /// Initializes a new instance of the <see cref="IndexListing"/> class.
        /// </summary>
        public IndexListing(ILuceneClient luceneClient, IPageUrlGenerator pageUrlGenerator)
        {
            this.luceneClient = luceneClient;
            this.pageUrlGenerator = pageUrlGenerator;
        }


        /// <inheritdoc/>
        public override Task ConfigurePage()
        {
            if (!IndexStore.Instance.GetAllIndexes().Any())
            {
                PageConfiguration.Callouts = new List<CalloutConfiguration>
                {
                    new CalloutConfiguration
                    {
                        Headline = LocalizationService.GetString("integrations.lucene.listing.noindexes.headline"),
                        Content = LocalizationService.GetString("integrations.lucene.listing.noindexes.description"),
                        ContentAsHtml = true,
                        Type = CalloutType.FriendlyWarning,
                        Placement = CalloutPlacement.OnDesk
                    }
                };
            }

            PageConfiguration.ColumnConfigurations
                .AddColumn(nameof(LuceneIndexStatisticsViewModel.Name), LocalizationService.GetString("integrations.lucene.listing.columns.name"), defaultSortDirection: SortTypeEnum.Asc, searchable: true)
                .AddColumn(nameof(LuceneIndexStatisticsViewModel.Entries), LocalizationService.GetString("integrations.lucene.listing.columns.entries"))
                //.AddColumn(nameof(LuceneIndexStatisticsViewModel.LastBuildTimes), LocalizationService.GetString("integrations.lucene.listing.columns.buildtime"))
                .AddColumn(nameof(LuceneIndexStatisticsViewModel.UpdatedAt), LocalizationService.GetString("integrations.lucene.listing.columns.updatedat"));

            PageConfiguration.TableActions.AddCommand(LocalizationService.GetString("integrations.lucene.listing.commands.rebuild"), nameof(Rebuild), Icons.RotateRight);

            return base.ConfigurePage();
        }


        /// <summary>
        /// A page command which displays details about an index.
        /// </summary>
        /// <param name="id">The ID of the row that was clicked, which corresponds with the internal
        /// <see cref="LuceneIndex.Identifier"/> to display.</param>
        [PageCommand]
        public Task<INavigateResponse> RowClick(int id)
        => Task.FromResult(NavigateTo(pageUrlGenerator.GenerateUrl(typeof(IndexedContent), id.ToString())));


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
                    .AddErrorMessage(string.Format(LocalizationService.GetString("integrations.lucene.error.noindex"), id));
            }

            try
            {
                await luceneClient.Rebuild(index.IndexName, cancellationToken);
                return ResponseFrom(result)
                    .AddSuccessMessage(LocalizationService.GetString("integrations.lucene.listing.messages.rebuilding"));
            }
            catch (Exception ex)
            {
                EventLogService.LogException(nameof(IndexListing), nameof(Rebuild), ex);
                return ResponseFrom(result)
                    .AddErrorMessage(string.Format(LocalizationService.GetString("integrations.lucene.listing.messages.rebuilderror"), index.IndexName));
            }

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
                    IndexStore.Instance.GetAllIndexes().Any(index => index.IndexName.Equals(stat.Name, StringComparison.OrdinalIgnoreCase)));

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
                EventLogService.LogException(nameof(IndexListing), nameof(LoadData), ex);
                return new LoadDataResult
                {
                    Rows = Enumerable.Empty<Row>(),
                    TotalCount = 0
                };
            }
        }


        private static void AddMissingStatistics(ref ICollection<LuceneIndexStatisticsViewModel> statistics)
        {
            foreach (string indexName in IndexStore.Instance.GetAllIndexes().Select(i => i.IndexName))
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
                        //new StringCell
                        //{
                        //    Value = statistics.LastBuildTimes.ToString()
                        //},
                        new StringCell
                        {
                            Value = statistics.UpdatedAt.ToString()
                        },
                        new ActionCell
                        {
                            Actions = new List<Action>
                            {
                                new Action(ActionType.Command)
                                {
                                    Title = LocalizationService.GetString("integrations.lucene.listing.commands.rebuild"),
                                    Label = LocalizationService.GetString("integrations.lucene.listing.commands.rebuild"),
                                    Icon = Icons.RotateRight,
                                    Parameter = nameof(Rebuild)
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
}
