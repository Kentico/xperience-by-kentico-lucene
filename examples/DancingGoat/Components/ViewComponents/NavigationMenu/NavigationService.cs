using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CMS.Helpers;
using CMS.Websites;
using CMS.Websites.Routing;

using DancingGoat.Models;

namespace DancingGoat.ViewComponents
{
    public class NavigationService
    {
        private readonly NavigationItemRepository navigationItemRepository;
        private readonly IWebPageUrlRetriever webPageUrlRetriever;
        private readonly IProgressiveCache progressiveCache;
        private readonly IWebsiteChannelContext websiteChannelContext;


        public NavigationService(
            NavigationItemRepository navigationItemRepository,
            IWebPageUrlRetriever webPageUrlRetriever,
            IProgressiveCache progressiveCache,
            IWebsiteChannelContext websiteChannelContext)
        {
            this.navigationItemRepository = navigationItemRepository;
            this.webPageUrlRetriever = webPageUrlRetriever;
            this.progressiveCache = progressiveCache;
            this.websiteChannelContext = websiteChannelContext;
        }


        public async Task<IEnumerable<NavigationItemViewModel>> GetNavigationItemViewModels(string languageName, CancellationToken cancellationToken = default)
        {
            var navigationItems = (await navigationItemRepository.GetNavigationItems(languageName, cancellationToken))
                .ToList();

            var menuItemGuids = navigationItems
                .Select(navigationItem => navigationItem.NavigationItemLink.First().WebPageGuid)
                .ToList();

            var navigationModels = await GetModelsCached(navigationItems, menuItemGuids, languageName, cancellationToken);

            return navigationModels;
        }


        private async Task<IEnumerable<NavigationItemViewModel>> GetModelsCached(List<NavigationItem> navigationItems, List<Guid> menuItemGuids, string languageName, CancellationToken cancellationToken)
        {
            var cacheSettings = new CacheSettings(5, websiteChannelContext.WebsiteChannelName, nameof(GetNavigationItemViewModels), languageName);

            return await progressiveCache.LoadAsync(async (settings, cancellationToken) =>
            {
                var urls = await webPageUrlRetriever.Retrieve(menuItemGuids, websiteChannelContext.WebsiteChannelName, languageName, cancellationToken: cancellationToken);

                var navigationModels = navigationItems
                        .Where(navigationItem => urls.ContainsKey(navigationItem.NavigationItemLink.First().WebPageGuid))
                        .Select(navigationItem =>
                            new NavigationItemViewModel(
                                navigationItem.NavigationItemName,
                                urls[navigationItem.NavigationItemLink.First().WebPageGuid].RelativePath
                            ));

                if (cacheSettings.Cached = navigationModels != null && navigationModels.Any())
                {
                    var cacheKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    foreach (var key in menuItemGuids)
                    {
                        cacheKeys.Add(CacheHelper.BuildCacheItemName(new[] { "webpageitem", "byguid", key.ToString() }, false));
                    }

                    cacheKeys.Add(CacheHelper.BuildCacheItemName(new[] { "webpageitem", "bychannel", websiteChannelContext.WebsiteChannelName, "childrenofpath", DancingGoatConstants.NAVIGATION_MENU_FOLDER_PATH }));

                    cacheSettings.CacheDependency = CacheHelper.GetCacheDependency(cacheKeys);
                }

                return navigationModels;
            }, cacheSettings, cancellationToken);
        }
    }
}
