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
    /// Represents a collection of landing pages.
    /// </summary>
    public class LandingPageRepository : ContentRepositoryBase
    {
        public LandingPageRepository(IWebsiteChannelContext websiteChannelContext, IContentQueryExecutor executor, IProgressiveCache cache)
            : base(websiteChannelContext, executor, cache)
        {
        }

        /// <summary>
        /// Returns <see cref="LandingPage"/> content item.
        /// </summary>
        /// <param name="webPageItemId">Web page item ID.</param>
        /// <param name="languageName">Language name.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task<LandingPage> GetLandingPage(int webPageItemId, string languageName, CancellationToken cancellationToken = default)
        {
            var queryBuilder = GetQueryBuilder(webPageItemId, languageName);

            var cacheSettings = new CacheSettings(5, WebsiteChannelContext.WebsiteChannelName, nameof(LandingPage), webPageItemId, languageName);

            var result = await GetCachedQueryResult<LandingPage>(queryBuilder, null, cacheSettings, GetDependencyCacheKeys, cancellationToken);

            return result.FirstOrDefault();
        }


        private ContentItemQueryBuilder GetQueryBuilder(int webPageItemId, string languageName)
        {
            return new ContentItemQueryBuilder()
                    .ForContentType(LandingPage.CONTENT_TYPE_NAME, config => config
                        .ForWebsite(WebsiteChannelContext.WebsiteChannelName, includeUrlPath: false)
                        .Where(where => where
                            .WhereEquals(nameof(IWebPageContentQueryDataContainer.WebPageItemID), webPageItemId))
                        .TopN(1))
                    .InLanguage(languageName);
        }


        private static Task<ISet<string>> GetDependencyCacheKeys(IEnumerable<LandingPage> confirmationPages, CancellationToken cancellationToken)
        {
            var dependencyCacheKeys = new HashSet<string>();

            var confirmationPage = confirmationPages.FirstOrDefault();

            if (confirmationPage != null)
            {
                dependencyCacheKeys.Add(CacheHelper.BuildCacheItemName(new[] { "webpageitem", "byid", confirmationPage.SystemFields.WebPageItemID.ToString() }, false));
            }

            return Task.FromResult<ISet<string>>(dependencyCacheKeys);
        }
    }
}
