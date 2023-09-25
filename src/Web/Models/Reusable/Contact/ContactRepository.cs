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
    /// Represents a collection of contact information.
    /// </summary>
    public class ContactRepository : ContentRepositoryBase
    {
        public ContactRepository(IWebsiteChannelContext websiteChannelContext, IContentQueryExecutor executor, IWebPageQueryResultMapper mapper, IProgressiveCache cache)
            : base(websiteChannelContext, executor, mapper, cache)
        {
        }


        /// <summary>
        /// Returns the first <see cref="Contact"/> content item.
        /// </summary>
        public async Task<Contact> GetContact(CancellationToken cancellationToken = default)
        {
            var cacheSettings = new CacheSettings(5, WebsiteConstants.WEBSITE_CHANNEL_NAME, nameof(Contact));

            var result = await GetCachedQueryResult<Contact>(GetContactQuery(), null, cacheSettings, GetDependencyCacheKeys, cancellationToken);

            return result.FirstOrDefault();
        }


        private ContentItemQueryBuilder GetContactQuery()
        {
            return new ContentItemQueryBuilder()
                    .ForContentType(Contact.CONTENT_TYPE_NAME, config => config.TopN(1));
        }


        private ISet<string> GetDependencyCacheKeys(IEnumerable<Contact> contacts)
        {
            var dependencyCacheKeys = new HashSet<string>();

            var contact = contacts.FirstOrDefault();

            if (contact != null)
            {
                dependencyCacheKeys.Add(CacheHelper.BuildCacheItemName(new[] { "contentitem", "byid", contact.SystemFields.ContentItemID.ToString() }, false));
            }

            return dependencyCacheKeys;
        }
    }
}