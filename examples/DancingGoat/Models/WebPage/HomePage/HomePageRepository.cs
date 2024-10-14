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
    /// Represents a collection of home pages.
    /// </summary>
    public class HomePageRepository : ContentRepositoryBase
    {
        private readonly IWebPageLinkedItemsDependencyAsyncRetriever webPageLinkedItemsDependencyRetriever;


        /// <summary>
        /// Initializes new instance of <see cref="HomePageRepository"/>.
        /// </summary>
        public HomePageRepository(IWebsiteChannelContext websiteChannelContext, IContentQueryExecutor executor, IProgressiveCache cache, IWebPageLinkedItemsDependencyAsyncRetriever webPageLinkedItemsDependencyRetriever)
            : base(websiteChannelContext, executor, cache)
        {
            this.webPageLinkedItemsDependencyRetriever = webPageLinkedItemsDependencyRetriever;
        }


        /// <summary>
        /// Returns <see cref="HomePage"/> content item.
        /// </summary>
        public async Task<HomePage> GetHomePage(int webPageItemId, string languageName, CancellationToken cancellationToken = default)
        {
            var queryBuilder = GetQueryBuilder(webPageItemId, languageName);

            var cacheSettings = new CacheSettings(5, WebsiteChannelContext.WebsiteChannelName, nameof(HomePage), languageName);

            var result = await GetCachedQueryResult<HomePage>(queryBuilder, null, cacheSettings, GetDependencyCacheKeys, cancellationToken);

            return result.FirstOrDefault();
        }


        private ContentItemQueryBuilder GetQueryBuilder(int webPageItemId, string languageName)
        {
            return new ContentItemQueryBuilder()
                    .ForContentType(HomePage.CONTENT_TYPE_NAME,
                        config => config
                                .WithLinkedItems(4)
                                .ForWebsite(WebsiteChannelContext.WebsiteChannelName)
                                .Where(where => where.WhereEquals(nameof(IWebPageContentQueryDataContainer.WebPageItemID), webPageItemId))
                                .TopN(1))
                    .InLanguage(languageName);
        }


        private async Task<ISet<string>> GetDependencyCacheKeys(IEnumerable<HomePage> homePages, CancellationToken cancellationToken)
        {
            var homePage = homePages.FirstOrDefault();

            if (homePage == null)
            {
                return new HashSet<string>();
            }

            return (await webPageLinkedItemsDependencyRetriever.Get(homePage.SystemFields.WebPageItemID, 4, cancellationToken))
                .Concat(GetCacheByGuidKeys(homePage.HomePageArticlesSection.Select(articlesSection => articlesSection.WebPageGuid)))
                .Append(CacheHelper.BuildCacheItemName(new[] { "webpageitem", "byid", homePage.SystemFields.WebPageItemID.ToString() }, false))
                .Append(CacheHelper.GetCacheItemName(null, WebsiteChannelInfo.OBJECT_TYPE, "byid", WebsiteChannelContext.WebsiteChannelID))
                .Append(CacheHelper.GetCacheItemName(null, ContentLanguageInfo.OBJECT_TYPE, "all"))
                .ToHashSet(StringComparer.InvariantCultureIgnoreCase);
        }


        private static IEnumerable<string> GetCacheByGuidKeys(IEnumerable<Guid> webPageGuids)
        {
            foreach (var guid in webPageGuids)
            {
                yield return CacheHelper.BuildCacheItemName(new[] { "webpageitem", "byguid", guid.ToString() }, false);
            }
        }
    }
}
