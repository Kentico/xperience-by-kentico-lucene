using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CMS.ContentEngine;
using CMS.Helpers;
using CMS.Websites;
using CMS.Websites.Routing;

using DancingGoat.Helpers;

namespace DancingGoat.Models
{
    /// <summary>
    /// Represents a collection of home pages.
    /// </summary>
    public class HomePageRepository : ContentRepositoryBase
    {
        /// <summary>
        /// Initializes new instance of <see cref="HomePageRepository"/>.
        /// </summary>
        public HomePageRepository(IWebsiteChannelContext websiteChannelContext, IContentQueryExecutor executor, IWebPageQueryResultMapper mapper, IProgressiveCache cache)
            : base(websiteChannelContext, executor, mapper, cache)
        {
        }


        /// <summary>
        /// Returns <see cref="HomePage"/> content item.
        /// </summary>
        public async Task<HomePage> GetHomePage(string languageName, CancellationToken cancellationToken = default)
        {
            var cacheSettings = new CacheSettings(5, WebsiteConstants.WEBSITE_CHANNEL_NAME, nameof(HomePage), languageName);

            var result = await GetCachedQueryResult<HomePage>(GetHomePageQuery(languageName), null, cacheSettings, (homePage) => GetDependencyCacheKeys(homePage), cancellationToken);

            return result.FirstOrDefault();
        }


        private static ContentItemQueryBuilder GetHomePageQuery(string languageName)
        {
            return new ContentItemQueryBuilder()
                    .ForContentType(HomePage.CONTENT_TYPE_NAME,
                    config =>
                        config
                            .WithLinkedItems(4)
                            .ForWebsite(WebsiteConstants.WEBSITE_CHANNEL_NAME, PathMatch.Single("/Home"))
                            .TopN(1))
                    .InLanguage(languageName);
        }


        private ISet<string> GetDependencyCacheKeys(IEnumerable<HomePage> homePages)
        {
            var homePage = homePages.FirstOrDefault();

            if (homePage == null)
            {
                return new HashSet<string>();
            }

            return GetCacheByIdKeys(homePage.HomePageBanner.Select(banner => banner.SystemFields.ContentItemID))
                .Concat(GetCacheByIdKeys(homePage.HomePageEvent.Select(pageEvent => pageEvent.SystemFields.ContentItemID)))
                .Concat(GetCacheByIdKeys(homePage.HomePageReference.Select(reference => reference.SystemFields.ContentItemID)))
                .Concat(GetCacheByIdKeys(homePage.HomePageCafes.Select(cafe => cafe.SystemFields.ContentItemID)))
                .Append(CacheHelper.BuildCacheItemName(new[] { "webpageitem", "byid", homePage.SystemFields.WebPageItemID.ToString() }, false))
                .Append(CacheHelper.GetCacheItemName(null, WebsiteChannelInfo.OBJECT_TYPE, "byid", WebsiteChannelContext.WebsiteChannelID))
                .Append(CacheHelper.GetCacheItemName(null, ContentLanguageInfo.OBJECT_TYPE, "all"))
                .ToHashSet(StringComparer.InvariantCultureIgnoreCase);
        }


        private static IEnumerable<string> GetCacheByIdKeys(IEnumerable<int> itemIds)
        {
            foreach (var id in itemIds)
            {
                yield return CacheHelper.BuildCacheItemName(new[] { "contentitem", "byid", id.ToString() }, false);
            }
        }
    }
}
