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
    public class ContactsPageRepository : ContentRepositoryBase
    {
        /// <summary>
        /// Initializes new instance of <see cref="ContactsPageRepository"/>.
        /// </summary>
        public ContactsPageRepository(IWebsiteChannelContext websiteChannelContext, IContentQueryExecutor executor, IProgressiveCache cache)
            : base(websiteChannelContext, executor, cache)
        {
        }


        /// <summary>
        /// Returns <see cref="ContactsPage"/> web page by tree path and language name.
        /// </summary>
        public async Task<ContactsPage> GetContactsPage(string treePath, string languageName, CancellationToken cancellationToken = default)
        {
            var queryBuilder = GetQueryBuilder(w => w.WhereEquals(nameof(IWebPageContentQueryDataContainer.WebPageItemTreePath), treePath), languageName);

            var cacheSettings = new CacheSettings(5, WebsiteChannelContext.WebsiteChannelName, nameof(ContactsPage), nameof(IWebPageContentQueryDataContainer.WebPageItemTreePath), treePath, languageName);

            var result = await GetCachedQueryResult<ContactsPage>(queryBuilder, null, cacheSettings, GetDependencyCacheKeys, cancellationToken);

            return result.FirstOrDefault();
        }


        /// <summary>
        /// Returns <see cref="ContactsPage"/> web page by ID and language name.
        /// </summary>
        public async Task<ContactsPage> GetContactsPage(int webPageItemId, string languageName, CancellationToken cancellationToken = default)
        {
            var queryBuilder = GetQueryBuilder(w => w.WhereEquals(nameof(IWebPageContentQueryDataContainer.WebPageItemID), webPageItemId), languageName);

            var cacheSettings = new CacheSettings(5, WebsiteChannelContext.WebsiteChannelName, nameof(ContactsPage), nameof(IWebPageContentQueryDataContainer.WebPageItemID), webPageItemId, languageName);

            var result = await GetCachedQueryResult<ContactsPage>(queryBuilder, null, cacheSettings, GetDependencyCacheKeys, cancellationToken);

            return result.FirstOrDefault();
        }


        private ContentItemQueryBuilder GetQueryBuilder(Action<WhereParameters> where, string languageName)
        {
            return new ContentItemQueryBuilder()
                    .ForContentType(ContactsPage.CONTENT_TYPE_NAME, config => config
                        .ForWebsite(WebsiteChannelContext.WebsiteChannelName)
                    .Where(where)
                    .TopN(1))
                    .InLanguage(languageName);
        }


        private static Task<ISet<string>> GetDependencyCacheKeys(IEnumerable<ContactsPage> contactsPages, CancellationToken cancellationToken)
        {
            var dependencyCacheKeys = new HashSet<string>();

            var contactsPage = contactsPages.FirstOrDefault();
            if (contactsPage != null)
            {
                dependencyCacheKeys.Add(CacheHelper.BuildCacheItemName(new[] { "webpageitem", "byid", contactsPage.SystemFields.WebPageItemID.ToString() }, false));
            }

            return Task.FromResult<ISet<string>>(dependencyCacheKeys);
        }
    }
}
