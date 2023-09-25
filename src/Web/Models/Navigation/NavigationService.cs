using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.DataEngine.CollectionExtensions;
using CMS.Helpers;
using CMS.Websites;
using CMS.Websites.Routing;

using DancingGoat.Helpers;

using Kentico.Content.Web.Mvc;

namespace DancingGoat.Models
{
    /// <summary>
    /// A service for retrieving the navigation menu items.
    /// </summary>
    public class NavigationService
    {
        private const string NAVIGATION_TITLE_FIELD_NAME = "NavigationTitle";


        private record NavigationWebPageItem
        {
            public int WebPageItemID;

            public string WebPageItemTitle;

            public string WebPageItemUrlPath;

            public string WebPageItemTreePath;
        }


        private readonly IContentQueryExecutor contentQueryExecutor;
        private readonly IWebsiteChannelContext websiteChannelContext;
        private readonly IWebPageUrlRetriever webPageUrlRetriever;
        private readonly IProgressiveCache progressiveCache;
        private readonly ICurrentLanguageRetriever currentLanguageRetriever;


        /// <summary>
        /// Initializes a new instance of the <see cref="NavigationService"/> class.
        /// </summary>
        public NavigationService(IContentQueryExecutor contentQueryExecutor, IWebsiteChannelContext websiteChannelContext, IWebPageUrlRetriever webPageUrlRetriever, IProgressiveCache progressiveCache, ICurrentLanguageRetriever currentLanguageRetriever)
        {
            this.contentQueryExecutor = contentQueryExecutor;
            this.websiteChannelContext = websiteChannelContext;
            this.webPageUrlRetriever = webPageUrlRetriever;
            this.progressiveCache = progressiveCache;
            this.currentLanguageRetriever = currentLanguageRetriever;
        }


        /// <summary>
        /// Returns an enumerable collection of navigation items ordered by the content tree order and level.
        /// </summary>
        public async Task<IEnumerable<NavigationItemViewModel>> GetNavigationItems(CancellationToken cancellationToken = default)
        {
            var languageName = currentLanguageRetriever.Get();

            return await progressiveCache.LoadAsync(async (cacheSettings) =>
            {
                var navigationItems = (await GetNavigationItemsInternal(cancellationToken)).ToList();

                if (cacheSettings.Cached = navigationItems.Any())
                {
                    cacheSettings.CacheDependency = CacheHelper.GetCacheDependency(GetDependencyCacheKeys(navigationItems));
                }

                var models = new List<NavigationItemViewModel>();

                foreach (var item in navigationItems)
                {
                    models.Add(await GetModel(item, languageName, cancellationToken));
                }

                return models;
            }, new CacheSettings(60, WebsiteConstants.WEBSITE_CHANNEL_NAME, "navigationitems", languageName));
        }


        private Task<IEnumerable<NavigationWebPageItem>> GetNavigationItemsInternal(CancellationToken cancellationToken)
        {
            var builder = new ContentItemQueryBuilder()
                .Parameters(
                    config => config
                        .Columns(nameof(WebPageFields.WebPageItemID), nameof(WebPageFields.WebPageUrlPath), nameof(WebPageFields.WebPageItemTreePath), NAVIGATION_TITLE_FIELD_NAME)
                        .Where(where => where.WhereNull(nameof(IWebPageContentQueryDataContainer.WebPageItemParentID))
                        .WhereNotEmpty(NAVIGATION_TITLE_FIELD_NAME))
                        .OrderBy(nameof(IWebPageContentQueryDataContainer.WebPageItemOrder)));

            AddContentTypesToQuery(builder);

            builder.InLanguage(currentLanguageRetriever.Get());

            return contentQueryExecutor.GetWebPageResult(
                builder,
                container => new NavigationWebPageItem
                {
                    WebPageItemID = container.WebPageItemID,
                    WebPageItemTitle = container.GetValue<string>(NAVIGATION_TITLE_FIELD_NAME),
                    WebPageItemUrlPath = container.WebPageUrlPath,
                    WebPageItemTreePath = container.WebPageItemTreePath
                }, options: new ContentQueryExecutionOptions
                {
                    ForPreview = websiteChannelContext.IsPreview,
                    IncludeSecuredItems = websiteChannelContext.IsPreview,
                },
                cancellationToken
            );
        }


        private static void AddContentTypesToQuery(ContentItemQueryBuilder builder)
        {
            var contentTypes = DataClassInfoProvider.GetClasses()
                .WhereEquals(nameof(DataClassInfo.ClassContentTypeType), ClassContentTypeType.WEBSITE)
                .Columns(nameof(DataClassInfo.ClassName))
                .GetListResult<string>();

            foreach (var contentType in contentTypes)
            {
                builder.ForContentType(contentType, config => config.ForWebsite(WebsiteConstants.WEBSITE_CHANNEL_NAME, includeUrlPath: true));
            }
        }


        private async Task<NavigationItemViewModel> GetModel(NavigationWebPageItem navigationWebPageItem, string languageName, CancellationToken cancellationToken)
        {
            return new NavigationItemViewModel(
                    navigationWebPageItem.WebPageItemTitle,
                    (await webPageUrlRetriever.Retrieve(navigationWebPageItem.WebPageItemID, languageName, cancellationToken: cancellationToken)).RelativePath
                );
        }


        private static ISet<string> GetDependencyCacheKeys(IEnumerable<NavigationWebPageItem> navigationItems)
        {
            var cacheKeys = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

            foreach (var item in navigationItems)
            {
                cacheKeys.AddRangeToSet(GetDependencyCacheKeysInternal(item));
            }

            cacheKeys.Add(CacheHelper.GetCacheItemName(null, ContentLanguageInfo.OBJECT_TYPE, "all"));

            return cacheKeys;
        }


        private static IEnumerable<string> GetDependencyCacheKeysInternal(NavigationWebPageItem navigationItem)
        {
            // Include path dependency to flush cache when a new child page is created or page order is changed
            return new[] {
                CacheHelper.BuildCacheItemName(new[] { "webpageitem", "byid", navigationItem.WebPageItemID.ToString() }, false),
                CacheHelper.BuildCacheItemName(new[] { "webpageitem", "bychannel", WebsiteConstants.WEBSITE_CHANNEL_NAME, "bypath", navigationItem.WebPageItemTreePath }, false),
                CacheHelper.BuildCacheItemName(new[] { "webpageitem", "bychannel", WebsiteConstants.WEBSITE_CHANNEL_NAME, "childrenofpath", DataHelper.GetParentPath(navigationItem.WebPageItemTreePath) }, false),
            };
        }
    }
}