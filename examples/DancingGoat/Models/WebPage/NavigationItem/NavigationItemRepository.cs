using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CMS.ContentEngine;
using CMS.Helpers;
using CMS.Websites;
using CMS.Websites.Routing;

namespace DancingGoat.Models
{
    /// <summary>
    /// Represents a collection of navigation items.
    /// </summary>
    public class NavigationItemRepository : ContentRepositoryBase
    {
        /// <summary>
        /// Initializes new instance of <see cref="NavigationItemRepository"/>.
        /// </summary>
        public NavigationItemRepository(IWebsiteChannelContext websiteChannelContext, IContentQueryExecutor executor, IProgressiveCache cache)
            : base(websiteChannelContext, executor, cache)
        {
        }


        /// <summary>
        /// Returns list of <see cref="NavigationItem"/> content items representing navigation menu.
        /// </summary>
        public async Task<IEnumerable<NavigationItem>> GetNavigationItems(string languageName, CancellationToken cancellationToken)
        {
            var query = new ContentItemQueryBuilder()
                .ForContentType(NavigationItem.CONTENT_TYPE_NAME, config => config
                    .ForWebsite(WebsiteChannelContext.WebsiteChannelName, PathMatch.Children(DancingGoatConstants.NAVIGATION_MENU_FOLDER_PATH), includeUrlPath: false)
                    .OrderBy(nameof(IWebPageContentQueryDataContainer.WebPageItemOrder)))
                .InLanguage(languageName);

            var cacheSettings = new CacheSettings(5, WebsiteChannelContext.WebsiteChannelName, nameof(GetNavigationItems), languageName);

            return await GetCachedQueryResult<NavigationItem>(query, null, cacheSettings, GetDependencyCacheKeys, cancellationToken);
        }


        private Task<ISet<string>> GetDependencyCacheKeys(IEnumerable<NavigationItem> navigationItems, CancellationToken cancellationToken)
        {
            if (navigationItems == null)
            {
                return Task.FromResult<ISet<string>>(new HashSet<string>());
            }

            var dependencyCacheKeys = GetCacheKeys(navigationItems.Select(navItem => navItem.SystemFields.WebPageItemID))
                .Append(CacheHelper.BuildCacheItemName(new[] { "webpageitem", "bychannel", WebsiteChannelContext.WebsiteChannelName, "childrenofpath", DancingGoatConstants.NAVIGATION_MENU_FOLDER_PATH }))
                .ToHashSet(StringComparer.InvariantCultureIgnoreCase);

            return Task.FromResult<ISet<string>>(dependencyCacheKeys);
        }


        private static IEnumerable<string> GetCacheKeys(IEnumerable<int> itemIds)
        {
            foreach (int id in itemIds)
            {
                yield return CacheHelper.BuildCacheItemName(new[] { "webpageitem", "byid", id.ToString() }, false);
            }
        }
    }
}
