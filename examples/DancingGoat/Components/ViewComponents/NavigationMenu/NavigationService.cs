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
    public class NavigationService
    {
        private readonly IContentRetriever contentRetriever;
        private readonly IWebPageUrlRetriever webPageUrlRetriever;
        private readonly IWebsiteChannelContext websiteChannelContext;
        private readonly IProgressiveCache progressiveCache;

        public NavigationService(
            IContentRetriever contentRetriever,
            IWebPageUrlRetriever webPageUrlRetriever,
            IWebsiteChannelContext websiteChannelContext,
            IProgressiveCache progressiveCache)
        {
            this.contentRetriever = contentRetriever;
            this.webPageUrlRetriever = webPageUrlRetriever;
            this.websiteChannelContext = websiteChannelContext;
            this.progressiveCache = progressiveCache;
        }


        public async Task<IEnumerable<NavigationItemViewModel>> GetSiteNavigationItemViewModels(string languageName, CancellationToken cancellationToken = default)
        {
            return await GetNavigationItemViewModelsInternal(DancingGoatConstants.SITE_NAVIGATION_MENU_TREE_PATH, languageName, cancellationToken);
        }


        public async Task<IEnumerable<NavigationItemViewModel>> GetStoreNavigationItemViewModels(string languageName, CancellationToken cancellationToken = default)
        {
            return await GetNavigationItemViewModelsInternal(DancingGoatConstants.STORE_NAVIGATION_MENU_TREE_PATH, languageName, cancellationToken);
        }


        public async Task<IEnumerable<NavigationItemViewModel>> GetNavigationItemViewModelsInternal(string treePath, string languageName, CancellationToken cancellationToken = default)
        {
            using (var collector = new CacheDependencyCollector())
            {
                return await progressiveCache.LoadAsync(async (cacheSettings, cancellationToken) =>
                {
                    var navigationItems = (await contentRetriever.RetrievePages<NavigationItem>(
                        new RetrievePagesParameters
                        {
                            PathMatch = PathMatch.Children(treePath, 1)
                        },
                        query => query.OrderBy(nameof(IWebPageContentQueryDataContainer.WebPageItemOrder)),
                        RetrievalCacheSettings.CacheDisabled,
                        cancellationToken
                    )).ToList();

                    var urls = await webPageUrlRetriever
                        .Retrieve([.. navigationItems.SelectMany(x => x.NavigationItemLink.Select(y => y.WebPageGuid))],
                            websiteChannelContext.WebsiteChannelName, languageName, websiteChannelContext.IsPreview, cancellationToken);

                    cacheSettings.CacheDependency = collector.GetCacheDependency();

                    return navigationItems.Select(x => new NavigationItemViewModel(
                        x.NavigationItemName,
                        urls[x.NavigationItemLink.First().WebPageGuid].RelativePath
                    ));
                },
                new CacheSettings(10, $"NavigationItems_{treePath}"), cancellationToken);
            }
        }
    }
}
