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
    /// Represents a collection of confirmation pages.
    /// </summary>
    public class ConfirmationPageRepository : ContentRepositoryBase
    {
        public ConfirmationPageRepository(IWebsiteChannelContext websiteChannelContext, IContentQueryExecutor executor, IWebPageQueryResultMapper mapper, IProgressiveCache cache)
            : base(websiteChannelContext, executor, mapper, cache)
        {
        }


        /// <summary>
        /// Returns <see cref="ConfirmationPage"/> content item.
        /// </summary>
        /// <param name="webPageItemId">Web page item ID.</param>
        /// <param name="languageName">Language name.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task<ConfirmationPage> GetConfirmationPage(int webPageItemId, string languageName, CancellationToken cancellationToken = default)
        {
            var cacheSettings = new CacheSettings(5, WebsiteConstants.WEBSITE_CHANNEL_NAME, nameof(ConfirmationPage), webPageItemId, languageName);

            var result = await GetCachedQueryResult<ConfirmationPage>(GetConfirmationPageQuery(webPageItemId, languageName), null, cacheSettings, GetDependencyCacheKeys, cancellationToken);

            return result.FirstOrDefault();
        }


        private static ContentItemQueryBuilder GetConfirmationPageQuery(int webPageItemId, string languageName)
        {
            return new ContentItemQueryBuilder()
                    .ForContentType(ConfirmationPage.CONTENT_TYPE_NAME, config => config
                        .ForWebsite(WebsiteConstants.WEBSITE_CHANNEL_NAME)
                        .Where(where => where
                            .WhereEquals(nameof(IWebPageContentQueryDataContainer.WebPageItemID), webPageItemId))
                        .TopN(1))
                    .InLanguage(languageName);
        }


        private static ISet<string> GetDependencyCacheKeys(IEnumerable<ConfirmationPage> confirmationPages)
        {
            var dependencyCacheKeys = new HashSet<string>();

            var confirmationPage = confirmationPages.FirstOrDefault();

            if (confirmationPage != null)
            {
                dependencyCacheKeys.Add(CacheHelper.BuildCacheItemName(new[] { "webpageitem", "byid", confirmationPage.SystemFields.WebPageItemID.ToString() }, false));
            }

            return dependencyCacheKeys;
        }
    }
}
