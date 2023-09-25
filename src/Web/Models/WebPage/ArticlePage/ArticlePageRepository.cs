using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Helpers;
using CMS.Websites;
using CMS.Websites.Routing;

using DancingGoat.Helpers;

namespace DancingGoat.Models
{
    /// <summary>
    /// Represents a collection of article pages.
    /// </summary>
    public class ArticlePageRepository : ContentRepositoryBase
    {
        /// <summary>
        /// Initializes new instance of <see cref="ArticlePageRepository"/>.
        /// </summary>
        public ArticlePageRepository(IWebsiteChannelContext websiteChannelContext, IContentQueryExecutor executor, IWebPageQueryResultMapper mapper, IProgressiveCache cache)
            : base(websiteChannelContext, executor, mapper, cache)
        {
        }


        /// <summary>
        /// Returns list of <see cref="ArticlePage"/> web pages.
        /// </summary>
        public async Task<IEnumerable<ArticlePage>> GetArticles(string languageName, bool includeSecuredItems, int topN = 0, CancellationToken cancellationToken = default)
        {
            var options = new ContentQueryExecutionOptions
            {
                IncludeSecuredItems = includeSecuredItems
            };

            var cacheSettings = new CacheSettings(5, WebsiteConstants.WEBSITE_CHANNEL_NAME, "/Articles", languageName, includeSecuredItems, topN);

            return await GetCachedQueryResult<ArticlePage>(GetArticlesQuery(topN, languageName), options, cacheSettings, (articles) => GetDependencyCacheKeys(articles), cancellationToken);
        }


        /// <summary>
        /// Returns list of <see cref="ArticlePage"/> content items with guids passed in parameter.
        /// </summary>
        public async Task<IEnumerable<ArticlePage>> GetArticles(ICollection<Guid> guids, string languageName, CancellationToken cancellationToken = default)
        {
            var options = new ContentQueryExecutionOptions
            {
                IncludeSecuredItems = true
            };

            var cacheSettings = new CacheSettings(5, WebsiteConstants.WEBSITE_CHANNEL_NAME, "/Articles", languageName, guids.GetHashCode());

            return await GetCachedQueryResult<ArticlePage>(GetArticlesQuery(guids, languageName), options, cacheSettings, (articles) => GetDependencyCacheKeys(articles), cancellationToken);
        }


        /// <summary>
        /// Returns <see cref="ArticlePage"/> web page by ID and language name.
        /// </summary>
        public async Task<ArticlePage> GetArticle(int id, string languageName, CancellationToken cancellationToken = default)
        {
            var options = new ContentQueryExecutionOptions
            {
                IncludeSecuredItems = true
            };

            var cacheSettings = new CacheSettings(5, WebsiteConstants.WEBSITE_CHANNEL_NAME, nameof(ArticlePage), id, languageName);

            var result = await GetCachedQueryResult<ArticlePage>(GetArticleQuery(id, languageName), options, cacheSettings, (articles) => GetDependencyCacheKeys(articles), cancellationToken);

            return result.FirstOrDefault();
        }


        /// <summary>
        /// Returns ID of Articles section identifier by a code name.
        /// </summary>
        public async Task<int> GetArticlesSectionWebPageItemId(string codeName)
        {
            var query = new ContentItemQueryBuilder()
                    .ForContentType(ArticlesSection.CONTENT_TYPE_NAME,
                    config =>
                        config
                            .ForWebsite(WebsiteConstants.WEBSITE_CHANNEL_NAME)
                            .Where(where => where.WhereEquals(nameof(WebPageFields.WebPageItemName), codeName))
                            .Columns(nameof(WebPageFields.WebPageItemID))
                    .TopN(1));

            var cacheSettings = new CacheSettings(5, WebsiteConstants.WEBSITE_CHANNEL_NAME, nameof(ArticlesSection));

            return (await GetCachedQueryResult<int>(query, null, (container) => GetValueResultSelector<int>(container, nameof(WebPageFields.WebPageItemID)), cacheSettings, (result) => GetArticlesSectionDependencyCacheKeys(result.First(), codeName), CancellationToken.None)).FirstOrDefault();
        }


        private static T GetValueResultSelector<T>(IContentQueryDataContainer container, string columName)
        {
            return container.GetValue<T>(columName);
        }


        private static ContentItemQueryBuilder GetArticlesQuery(int topN, string languageName)
        {
            return GetQueryBuilder(
                languageName,
                config => config
                    .WithLinkedItems(1)
                    .TopN(topN)
                    .OrderBy(OrderByColumn.Desc(nameof(ArticlePage.ArticlePagePublishDate)))
                    .ForWebsite(WebsiteConstants.WEBSITE_CHANNEL_NAME, PathMatch.Children("/Articles"), includeUrlPath: true));
        }


        private static ContentItemQueryBuilder GetArticlesQuery(ICollection<Guid> guids, string languageName)
        {
            return GetQueryBuilder(
                languageName,
                config => config
                    .WithLinkedItems(1)
                    .OrderBy(OrderByColumn.Desc(nameof(ArticlePage.ArticlePagePublishDate)))
                    .ForWebsite(WebsiteConstants.WEBSITE_CHANNEL_NAME, PathMatch.Children("/Articles"), includeUrlPath: true)
                    .Where(where => where.WhereIn(nameof(IWebPageContentQueryDataContainer.WebPageItemGUID), guids)));
        }


        private static ContentItemQueryBuilder GetArticleQuery(int id, string languageName)
        {
            return GetQueryBuilder(
                languageName,
                config => config
                    .WithLinkedItems(1)
                    .ForWebsite(WebsiteConstants.WEBSITE_CHANNEL_NAME, includeUrlPath: true)
                    .Where(where => where.WhereEquals(nameof(IWebPageContentQueryDataContainer.WebPageItemID), id)));
        }


        private static ContentItemQueryBuilder GetQueryBuilder(string languageName, Action<ContentTypeQueryParameters> configureQuery = null)
        {
            return new ContentItemQueryBuilder()
                    .ForContentType(ArticlePage.CONTENT_TYPE_NAME, configureQuery)
                    .InLanguage(languageName);
        }


        private ISet<string> GetDependencyCacheKeys(IEnumerable<ArticlePage> articles)
        {
            var cacheKeys = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

            foreach (var article in articles)
            {
                cacheKeys.UnionWith(GetDependencyCacheKeys(article));
            }

            cacheKeys.Add(CacheHelper.GetCacheItemName(null, WebsiteChannelInfo.OBJECT_TYPE, "byid", WebsiteChannelContext.WebsiteChannelID));

            return cacheKeys;
        }


        private static IEnumerable<string> GetDependencyCacheKeys(ArticlePage article)
        {
            if (article == null)
            {
                return Enumerable.Empty<string>();
            }

            return GetCacheByIdKeys(article.ArticlePageTeaser.Select(teaser => teaser.SystemFields.ContentItemID))
                .Append(CacheHelper.BuildCacheItemName(new[] { "webpageitem", "byid", article.SystemFields.WebPageItemID.ToString() }, false))
                .Append(CacheHelper.BuildCacheItemName(new[] { "webpageitem", "bychannel", WebsiteConstants.WEBSITE_CHANNEL_NAME, "bypath", article.SystemFields.WebPageItemTreePath }, false))
                .Append(CacheHelper.BuildCacheItemName(new[] { "webpageitem", "bychannel", WebsiteConstants.WEBSITE_CHANNEL_NAME, "childrenofpath", DataHelper.GetParentPath(article.SystemFields.WebPageItemTreePath) }, false))
                .Append(CacheHelper.GetCacheItemName(null, ContentLanguageInfo.OBJECT_TYPE, "all"));
        }


        private static IEnumerable<string> GetCacheByIdKeys(IEnumerable<int> itemIds)
        {
            foreach (var id in itemIds)
            {
                yield return CacheHelper.BuildCacheItemName(new[] { "contentitem", "byid", id.ToString() }, false);
            }
        }


        private static ISet<string> GetArticlesSectionDependencyCacheKeys(int articlesSectionId, string articlesSectionName)
        {
            var cacheKeys = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase)
            {
                CacheHelper.BuildCacheItemName(new[] { "webpageitem", "byid", articlesSectionId.ToString() }, false),
                CacheHelper.BuildCacheItemName(new[] { "webpageitem", "byname", articlesSectionName }, false)
            };

            return cacheKeys;
        }
    }
}
