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
    public class ArticlesSectionRepository : ContentRepositoryBase
    {
        /// <summary>
        /// Initializes new instance of <see cref="ArticlesSectionRepository"/>.
        /// </summary>
        public ArticlesSectionRepository(IWebsiteChannelContext websiteChannelContext, IContentQueryExecutor executor, IProgressiveCache cache)
            : base(websiteChannelContext, executor, cache)
        {
        }


        /// <summary>
        /// Returns <see cref="ArticlesSection"/> web page by ID and language name.
        /// </summary>
        public async Task<ArticlesSection> GetArticlesSection(int id, string languageName, CancellationToken cancellationToken = default)
        {
            var queryBuilder = GetQueryBuilder(id, languageName);

            var options = new ContentQueryExecutionOptions
            {
                IncludeSecuredItems = true
            };

            var cacheSettings = new CacheSettings(5, WebsiteChannelContext.WebsiteChannelName, nameof(ArticlesSection), id, languageName);

            var result = await GetCachedQueryResult<ArticlesSection>(queryBuilder, options, cacheSettings, GetDependencyCacheKeys, cancellationToken);

            return result.FirstOrDefault();
        }


        /// <summary>
        /// Returns <see cref="ArticlesSection"/> web page by GUID and language name.
        /// </summary>
        public async Task<ArticlesSection> GetArticlesSection(Guid guid, string languageName, CancellationToken cancellationToken = default)
        {
            var queryBuilder = GetQueryBuilder(guid, languageName);

            var options = new ContentQueryExecutionOptions
            {
                IncludeSecuredItems = true
            };

            var cacheSettings = new CacheSettings(5, WebsiteChannelContext.WebsiteChannelName, nameof(ArticlesSection), guid, languageName);

            var result = await GetCachedQueryResult<ArticlesSection>(queryBuilder, options, cacheSettings, GetDependencyCacheKeys, cancellationToken);

            return result.FirstOrDefault();
        }


        private Task<ISet<string>> GetDependencyCacheKeys(IEnumerable<ArticlesSection> articlesSections, CancellationToken cancellationToken)
        {
            var dependencyCacheKeys = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

            foreach (var articlesSection in articlesSections)
            {
                dependencyCacheKeys.UnionWith(GetDependencyCacheKeys(articlesSection));
            }

            dependencyCacheKeys.Add(CacheHelper.GetCacheItemName(null, WebsiteChannelInfo.OBJECT_TYPE, "byid", WebsiteChannelContext.WebsiteChannelID));

            return Task.FromResult<ISet<string>>(dependencyCacheKeys);
        }


        private static IEnumerable<string> GetDependencyCacheKeys(ArticlesSection articleSection)
        {
            if (articleSection == null)
            {
                return Enumerable.Empty<string>();
            }

            var cacheKeys = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase)
            {
                CacheHelper.BuildCacheItemName(new[] { "webpageitem", "byid", articleSection.SystemFields.WebPageItemID.ToString() }, false),
                CacheHelper.BuildCacheItemName(new[] { "webpageitem", "byguid", articleSection.SystemFields.WebPageItemGUID.ToString() }, false),
            };

            return cacheKeys;
        }


        private ContentItemQueryBuilder GetQueryBuilder(int id, string languageName)
        {
            return new ContentItemQueryBuilder()
                .ForContentType(ArticlesSection.CONTENT_TYPE_NAME,
                config => config
                        .ForWebsite(WebsiteChannelContext.WebsiteChannelName)
                        .Where(where => where.WhereEquals(nameof(WebPageFields.WebPageItemID), id))
                        .TopN(1))
                .InLanguage(languageName);
        }


        private ContentItemQueryBuilder GetQueryBuilder(Guid guid, string languageName)
        {
            return new ContentItemQueryBuilder()
                .ForContentType(ArticlesSection.CONTENT_TYPE_NAME,
                config => config
                        .ForWebsite(WebsiteChannelContext.WebsiteChannelName)
                        .Where(where => where.WhereEquals(nameof(IWebPageContentQueryDataContainer.WebPageItemGUID), guid))
                        .TopN(1))
                .InLanguage(languageName);
        }
    }
}
