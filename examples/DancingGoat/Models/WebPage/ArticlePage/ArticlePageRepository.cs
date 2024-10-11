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

namespace DancingGoat.Models
{
    /// <summary>
    /// Represents a collection of article pages.
    /// </summary>
    public class ArticlePageRepository : ContentRepositoryBase
    {
        private readonly IWebPageLinkedItemsDependencyAsyncRetriever webPageLinkedItemsDependencyRetriever;


        /// <summary>
        /// Initializes new instance of <see cref="ArticlePageRepository"/>.
        /// </summary>
        public ArticlePageRepository(
            IWebsiteChannelContext websiteChannelContext,
            IContentQueryExecutor executor,
            IProgressiveCache cache,
            IWebPageLinkedItemsDependencyAsyncRetriever webPageLinkedItemsDependencyRetriever)
            : base(websiteChannelContext, executor, cache)
        {
            this.webPageLinkedItemsDependencyRetriever = webPageLinkedItemsDependencyRetriever;
        }


        /// <summary>
        /// Returns list of <see cref="ArticlePage"/> web pages.
        /// </summary>
        public async Task<IEnumerable<ArticlePage>> GetArticles(string treePath, string languageName, bool includeSecuredItems, int topN = 0, CancellationToken cancellationToken = default)
        {
            var queryBuilder = GetQueryBuilder(topN, treePath, languageName);

            var options = new ContentQueryExecutionOptions
            {
                IncludeSecuredItems = includeSecuredItems
            };

            var cacheSettings = new CacheSettings(5, WebsiteChannelContext.WebsiteChannelName, treePath, languageName, includeSecuredItems, topN);

            return await GetCachedQueryResult<ArticlePage>(queryBuilder, options, cacheSettings, GetDependencyCacheKeys, cancellationToken);
        }


        /// <summary>
        /// Returns list of <see cref="ArticlePage"/> content items with guids passed in parameter.
        /// </summary>
        public async Task<IEnumerable<ArticlePage>> GetArticles(ICollection<Guid> guids, string languageName, CancellationToken cancellationToken = default)
        {
            var queryBuilder = GetQueryBuilder(guids, languageName);

            var options = new ContentQueryExecutionOptions { IncludeSecuredItems = true };

            var cacheSettings = new CacheSettings(5, WebsiteChannelContext.WebsiteChannelName, languageName, guids.GetHashCode());

            return await GetCachedQueryResult<ArticlePage>(queryBuilder, options, cacheSettings, GetDependencyCacheKeys, cancellationToken);
        }


        /// <summary>
        /// Returns <see cref="ArticlePage"/> web page by ID and language name.
        /// </summary>
        public async Task<ArticlePage> GetArticle(int id, string languageName, CancellationToken cancellationToken = default)
        {
            var queryBuilder = GetQueryBuilder(id, languageName);

            var options = new ContentQueryExecutionOptions
            {
                IncludeSecuredItems = true
            };

            var cacheSettings = new CacheSettings(5, WebsiteChannelContext.WebsiteChannelName, nameof(ArticlePage), id, languageName);

            var result = await GetCachedQueryResult<ArticlePage>(queryBuilder, options, cacheSettings, GetDependencyCacheKeys, cancellationToken);

            return result.FirstOrDefault();
        }



        private ContentItemQueryBuilder GetQueryBuilder(int topN, string treePath, string languageName)
        {
            return GetQueryBuilder(
                languageName,
                config => config
                    .WithLinkedItems(1)
                    .TopN(topN)
                    .OrderBy(OrderByColumn.Desc(nameof(ArticlePage.ArticlePagePublishDate)))
                    .ForWebsite(WebsiteChannelContext.WebsiteChannelName, PathMatch.Children(treePath)));
        }


        private ContentItemQueryBuilder GetQueryBuilder(ICollection<Guid> guids, string languageName)
        {
            return new ContentItemQueryBuilder().ForContentTypes(q =>
            {
                q.ForWebsite(guids)
                 .WithContentTypeFields()
                 .WithLinkedItems(1);
            }).InLanguage(languageName)
            .Parameters(q =>
                q.OrderBy(OrderByColumn.Desc(nameof(ArticlePage.ArticlePagePublishDate))));
        }


        private ContentItemQueryBuilder GetQueryBuilder(int id, string languageName)
        {
            return GetQueryBuilder(
                languageName,
                config => config
                    .WithLinkedItems(1)
                    .ForWebsite(WebsiteChannelContext.WebsiteChannelName)
                    .Where(where => where.WhereEquals(nameof(IWebPageContentQueryDataContainer.WebPageItemID), id)));
        }


        private static ContentItemQueryBuilder GetQueryBuilder(string languageName, Action<ContentTypeQueryParameters> configureQuery = null)
        {
            return new ContentItemQueryBuilder()
                    .ForContentType(ArticlePage.CONTENT_TYPE_NAME, configureQuery)
                    .InLanguage(languageName);
        }


        private async Task<ISet<string>> GetDependencyCacheKeys(IEnumerable<ArticlePage> articles, CancellationToken cancellationToken)
        {
            var dependencyCacheKeys = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

            foreach (var article in articles)
            {
                dependencyCacheKeys.UnionWith(GetDependencyCacheKeys(article));
            }

            dependencyCacheKeys.UnionWith(await webPageLinkedItemsDependencyRetriever.Get(articles.Select(articlePage => articlePage.SystemFields.WebPageItemID), 1, cancellationToken));
            dependencyCacheKeys.Add(CacheHelper.GetCacheItemName(null, WebsiteChannelInfo.OBJECT_TYPE, "byid", WebsiteChannelContext.WebsiteChannelID));

            return dependencyCacheKeys;
        }


        private IEnumerable<string> GetDependencyCacheKeys(ArticlePage article)
        {
            if (article == null)
            {
                return Enumerable.Empty<string>();
            }

            return new List<string>()
            {
                CacheHelper.BuildCacheItemName(new[] { "webpageitem", "byid", article.SystemFields.WebPageItemID.ToString() }, false),
                CacheHelper.BuildCacheItemName(new[] { "webpageitem", "bychannel", WebsiteChannelContext.WebsiteChannelName, "bypath", article.SystemFields.WebPageItemTreePath }, false),
                CacheHelper.BuildCacheItemName(new[] { "webpageitem", "bychannel", WebsiteChannelContext.WebsiteChannelName, "childrenofpath", DataHelper.GetParentPath(article.SystemFields.WebPageItemTreePath) }, false),
                CacheHelper.GetCacheItemName(null, ContentLanguageInfo.OBJECT_TYPE, "all")
            };
        }
    }
}
