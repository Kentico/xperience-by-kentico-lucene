using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CMS.Helpers;
using CMS.Websites;
using CMS.Websites.Routing;

using DancingGoat.Models;

using Kentico.Content.Web.Mvc;

namespace DancingGoat.ViewComponents
{
    /// <summary>
    /// Provides navigation menu items retrieved from the content tree.
    /// </summary>
    public sealed class NavigationService
    {
        private readonly IContentRetriever contentRetriever;
        private readonly IWebPageUrlRetriever webPageUrlRetriever;
        private readonly IWebsiteChannelContext websiteChannelContext;
        private readonly IProgressiveCache progressiveCache;
        private readonly ICacheDependencyBuilderFactory cacheDependencyBuilderFactory;


        /// <summary>
        /// Initializes a new instance of the <see cref="NavigationService"/> class.
        /// </summary>
        public NavigationService(
            IContentRetriever contentRetriever,
            IWebPageUrlRetriever webPageUrlRetriever,
            IWebsiteChannelContext websiteChannelContext,
            IProgressiveCache progressiveCache,
            ICacheDependencyBuilderFactory cacheDependencyBuilderFactory)
        {
            this.contentRetriever = contentRetriever;
            this.webPageUrlRetriever = webPageUrlRetriever;
            this.websiteChannelContext = websiteChannelContext;
            this.progressiveCache = progressiveCache;
            this.cacheDependencyBuilderFactory = cacheDependencyBuilderFactory;
        }


        /// <summary>
        /// Returns view models of the site navigation menu items.
        /// </summary>
        /// <param name="languageName">Language of the menu items.</param>
        /// <param name="cancellationToken">Cancellation instruction.</param>
        public async Task<IEnumerable<NavigationItemViewModel>> GetSiteNavigationItemViewModels(string languageName, CancellationToken cancellationToken = default)
        {
            return await GetNavigationItemViewModels(DancingGoatConstants.SITE_NAVIGATION_MENU_TREE_PATH, languageName, cancellationToken);
        }


        /// <summary>
        /// Returns view models of the store navigation menu items.
        /// </summary>
        /// <param name="languageName">Language of the menu items.</param>
        /// <param name="cancellationToken">Cancellation instruction.</param>
        public async Task<IEnumerable<NavigationItemViewModel>> GetStoreNavigationItemViewModels(string languageName, CancellationToken cancellationToken = default)
        {
            return await GetNavigationItemViewModels(DancingGoatConstants.STORE_NAVIGATION_MENU_TREE_PATH, languageName, cancellationToken);
        }


        private async Task<IEnumerable<NavigationItemViewModel>> GetNavigationItemViewModels(string treePath, string languageName, CancellationToken cancellationToken)
        {
            using var collector = new CacheDependencyCollector();

            if (websiteChannelContext.IsPreview)
            {
                return await GetNavigationItemViewModelsInternal(treePath, languageName, cancellationToken);
            }

            return await progressiveCache.LoadAsync(async (cacheSettings, cancellationToken) =>
            {
                var navigationItemViewModels = await GetNavigationItemViewModelsInternal(treePath, languageName, cancellationToken);

                cacheSettings.CacheDependency = collector.GetCacheDependency();

                return navigationItemViewModels;
            },
            new CacheSettings(10, nameof(NavigationService), websiteChannelContext.WebsiteChannelName, treePath, languageName), cancellationToken);
        }


        private async Task<List<NavigationItemViewModel>> GetNavigationItemViewModelsInternal(string treePath, string languageName, CancellationToken cancellationToken)
        {
            // Secured pages are intentionally excluded (retriever default) because the menu is cached per channel and language, not per user
            var navigationItems = (await contentRetriever.RetrievePages<NavigationItem>(
                new RetrievePagesParameters
                {
                    PathMatch = PathMatch.Children(treePath, 1),
                    LanguageName = languageName
                },
                query => query.OrderBy(nameof(IWebPageContentQueryDataContainer.WebPageItemOrder)),
                RetrievalCacheSettings.CacheDisabled,
                cancellationToken
            )).ToList();

            var linkedPageGuids = navigationItems
                .SelectMany(navigationItem => navigationItem.NavigationItemLink ?? [])
                .Select(link => link.WebPageGuid)
                .Distinct()
                .ToList();

            var urls = await webPageUrlRetriever.Retrieve(linkedPageGuids, websiteChannelContext.WebsiteChannelName,
                languageName, websiteChannelContext.IsPreview, cancellationToken);

            AddLinkedPagesCacheDependency(linkedPageGuids, languageName);

            var navigationItemViewModels = new List<NavigationItemViewModel>();
            foreach (var navigationItem in navigationItems)
            {
                var linkedPageGuid = navigationItem.NavigationItemLink?.FirstOrDefault()?.WebPageGuid;
                if (linkedPageGuid.HasValue && urls.TryGetValue(linkedPageGuid.Value, out var url))
                {
                    navigationItemViewModels.Add(new NavigationItemViewModel(navigationItem.NavigationItemName, url.RelativePath, NavigationActivePathResolver.Normalize(url.RelativePath)));
                }
            }

            return navigationItemViewModels;
        }


        private void AddLinkedPagesCacheDependency(IReadOnlyCollection<Guid> linkedPageGuids, string languageName)
        {
            if (linkedPageGuids.Count == 0)
            {
                return;
            }

            // The dependency collector only captures the navigation items themselves, not the linked pages whose URLs the menu renders
            var builder = cacheDependencyBuilderFactory.Create().ForWebPageItems();
            foreach (var linkedPageGuid in linkedPageGuids)
            {
                builder.ByGuid(linkedPageGuid, languageName);
            }

            CacheDependencyCollector.AddCacheDependency(builder.Builder().Build());
        }
    }
}
