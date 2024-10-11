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
    /// Represents a collection of links to social networks.
    /// </summary>
    public class SocialLinkRepository : ContentRepositoryBase
    {
        private readonly ILinkedItemsDependencyRetriever linkedItemsDependencyRetriever;


        public SocialLinkRepository(IWebsiteChannelContext websiteChannelContext, IContentQueryExecutor executor, IProgressiveCache cache, ILinkedItemsDependencyRetriever linkedItemsDependencyRetriever)
            : base(websiteChannelContext, executor, cache)
        {
            this.linkedItemsDependencyRetriever = linkedItemsDependencyRetriever;
        }


        /// <summary>
        /// Returns list of <see cref="SocialLink"/> content items.
        /// </summary>
        public async Task<IEnumerable<SocialLink>> GetSocialLinks(string languageName, CancellationToken cancellationToken = default)
        {
            var queryBuilder = GetQueryBuilder(languageName);

            var cacheSettings = new CacheSettings(5, WebsiteChannelContext.WebsiteChannelName, nameof(SocialLinkRepository), nameof(GetSocialLinks), languageName);

            return await GetCachedQueryResult<SocialLink>(queryBuilder, null, cacheSettings, GetDependencyCacheKeys, cancellationToken);
        }


        private static ContentItemQueryBuilder GetQueryBuilder(string languageName)
        {
            return new ContentItemQueryBuilder()
                    .ForContentType(SocialLink.CONTENT_TYPE_NAME, config => config.WithLinkedItems(1))
                    .InLanguage(languageName);
        }


        private Task<ISet<string>> GetDependencyCacheKeys(IEnumerable<SocialLink> socialLinks, CancellationToken cancellationToken)
        {
            var dependencyCacheKeys = GetCacheByIdKeys(socialLinks.Select(socialLink => socialLink.SystemFields.ContentItemID))
                .Concat(linkedItemsDependencyRetriever.Get(socialLinks.Select(link => link.SystemFields.ContentItemID), 1))
                .Append(CacheHelper.GetCacheItemName(null, ContentLanguageInfo.OBJECT_TYPE, "all"))
                .ToHashSet(StringComparer.InvariantCultureIgnoreCase);

            return Task.FromResult<ISet<string>>(dependencyCacheKeys);
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
