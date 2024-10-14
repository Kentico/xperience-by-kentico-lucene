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
    public class PrivacyPageRepository : ContentRepositoryBase
    {
        /// <summary>
        /// Initializes new instance of <see cref="PrivacyPageRepository"/>.
        /// </summary>
        public PrivacyPageRepository(IWebsiteChannelContext websiteChannelContext, IContentQueryExecutor executor, IProgressiveCache cache)
            : base(websiteChannelContext, executor, cache)
        {
        }


        /// <summary>
        /// Returns <see cref="PrivacyPage"/> web page by ID and language name.
        /// </summary>
        public async Task<PrivacyPage> GetPrivacyPage(int webPageItemId, string languageName, CancellationToken cancellationToken = default)
        {
            var queryBuilder = GetQueryBuilder(webPageItemId, languageName);

            var cacheSettings = new CacheSettings(5, WebsiteChannelContext.WebsiteChannelName, nameof(PrivacyPage), webPageItemId, languageName);

            var result = await GetCachedQueryResult<PrivacyPage>(queryBuilder, null, cacheSettings, GetDependencyCacheKeys, cancellationToken);

            return result.FirstOrDefault();
        }


        private ContentItemQueryBuilder GetQueryBuilder(int webPageItemId, string languageName)
        {
            return new ContentItemQueryBuilder()
                    .ForContentType(PrivacyPage.CONTENT_TYPE_NAME, config => config
                        .ForWebsite(WebsiteChannelContext.WebsiteChannelName, includeUrlPath: false)
                    .Where(where => where
                            .WhereEquals(nameof(IWebPageContentQueryDataContainer.WebPageItemID), webPageItemId))
                    .TopN(1))
                    .InLanguage(languageName);
        }


        private static Task<ISet<string>> GetDependencyCacheKeys(IEnumerable<PrivacyPage> privacyPages, CancellationToken cancellationToken)
        {
            var dependencyCacheKeys = new HashSet<string>();

            var privacyPage = privacyPages.FirstOrDefault();
            if (privacyPage != null)
            {
                dependencyCacheKeys.Add(CacheHelper.BuildCacheItemName(new[] { "webpageitem", "byid", privacyPage.SystemFields.WebPageItemID.ToString() }, false));
            }

            return Task.FromResult<ISet<string>>(dependencyCacheKeys);
        }
    }
}
