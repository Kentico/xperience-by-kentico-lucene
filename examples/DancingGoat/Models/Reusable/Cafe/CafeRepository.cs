using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CMS.ContentEngine;
using CMS.Helpers;
using CMS.Websites.Routing;

namespace DancingGoat.Models
{
    /// <summary>
    /// Represents a collection of cafes.
    /// </summary>
    public partial class CafeRepository : ContentRepositoryBase
    {
        private readonly ILinkedItemsDependencyAsyncRetriever linkedItemsDependencyRetriever;


        public CafeRepository(
            IWebsiteChannelContext websiteChannelContext,
            IContentQueryExecutor executor,
            IProgressiveCache cache,
            ILinkedItemsDependencyAsyncRetriever linkedItemsDependencyRetriever)
            : base(websiteChannelContext, executor, cache)
        {
            this.linkedItemsDependencyRetriever = linkedItemsDependencyRetriever;
        }

        /// <summary>
        /// Returns an enumerable collection of cafes.
        /// </summary>
        public async Task<IEnumerable<Cafe>> GetCafes(int count, string languageName, CancellationToken cancellationToken = default)
        {
            var queryBuilder = GetQueryBuilder(count, languageName);

            var cacheSettings = new CacheSettings(5, WebsiteChannelContext.WebsiteChannelName, nameof(CafeRepository), nameof(GetCafes), count, languageName);

            return await GetCachedQueryResult<Cafe>(queryBuilder, null, cacheSettings, GetDependencyCacheKeys, cancellationToken);
        }


        private static ContentItemQueryBuilder GetQueryBuilder(int count, string languageName)
        {
            return new ContentItemQueryBuilder()
                    .ForContentType(Cafe.CONTENT_TYPE_NAME,
                        config => config
                        .WithLinkedItems(1)
                        .TopN(count))
                    .InLanguage(languageName);
        }


        private async Task<ISet<string>> GetDependencyCacheKeys(IEnumerable<Cafe> cafes, CancellationToken cancellationToken)
        {
            var cafeIds = cafes.Select(cafe => cafe.SystemFields.ContentItemID);
            var dependencyCacheKeys = (await linkedItemsDependencyRetriever.Get(cafeIds, 1, cancellationToken))
                .ToHashSet(StringComparer.InvariantCultureIgnoreCase);
            dependencyCacheKeys.Add(CacheHelper.BuildCacheItemName(new[] { "contentitem", "bycontenttype", Cafe.CONTENT_TYPE_NAME }, false));

            return dependencyCacheKeys;
        }
    }
}
