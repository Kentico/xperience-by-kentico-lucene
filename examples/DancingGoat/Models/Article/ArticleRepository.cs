using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CMS.DocumentEngine;
using CMS.DocumentEngine.Types.DancingGoatCore;

using DancingGoat.Infrastructure;

using Kentico.Content.Web.Mvc;

namespace DancingGoat.Models
{
    /// <summary>
    /// Provides methods for retrieving pages of type Article.
    /// </summary>
    public class ArticleRepository
    {
        private readonly IPageRetriever pageRetriever;
        private readonly IPageDataContextRetriever pageDataContextRetriever;
        private readonly RepositoryCacheHelper repositoryCacheHelper;


        /// <summary>
        /// Initializes a new instance of the <see cref="ArticleRepository"/> class.
        /// </summary>
        /// <param name="pageRetriever">The pages retriever.</param>
        /// <param name="repositoryCacheHelper">Cache helper for repositories making working with cache easier.</param>
        public ArticleRepository(IPageRetriever pageRetriever, IPageDataContextRetriever pageDataContextRetriever, RepositoryCacheHelper repositoryCacheHelper)
        {
            this.pageRetriever = pageRetriever;
            this.pageDataContextRetriever = pageDataContextRetriever;
            this.repositoryCacheHelper = repositoryCacheHelper;
        }


        /// <summary>
        /// Returns an enumerable collection of articles ordered by the date of publication. The most recent articles come first.
        /// </summary>
        /// <param name="nodeAliasPath">The node alias path of the articles section in the content tree.</param>
        /// <param name="count">The number of articles to return. Use 0 as value to return all records.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task<IEnumerable<Article>> GetArticles(string nodeAliasPath, int count = 0, CancellationToken? cancellationToken = null)
        {
            const int maxLevel = 2;

            var articles = pageRetriever.Retrieve<Article>(
                query => query
                    .Path(nodeAliasPath, PathTypeEnum.Children)
                    .TopN(count)
                    .OrderByDescending("DocumentPublishFrom"),
                cache => cache
                    .Key($"{nameof(ArticleRepository)}|{nameof(GetArticles)}|{nodeAliasPath}|{count}")
                    // Include path dependency to flush cache when a new child page is created.
                    .Dependencies((_, builder) => builder.PagePath(nodeAliasPath, PathTypeEnum.Children)));

            return await repositoryCacheHelper.CacheData(async cancellationToken =>
            {
                var articlesWithLinkedItems = new List<Article>();

                foreach (var article in articles)
                {
                    articlesWithLinkedItems.Add(await article.WithLinkedItems(maxLevel, cancellationToken));
                }

                return articlesWithLinkedItems;
            }, cancellationToken ?? CancellationToken.None, $"{nameof(ArticleRepository)}|{nameof(GetArticles)}|WithLinkedItems|{nodeAliasPath}|{count}",
            () => articles.GetLinkedItemsCacheDependencyKeys(maxLevel, cancellationToken)
            );
            
        }


        /// <summary>
        /// Returns current article.
        /// </summary>
        /// <param name="maxLevel">Maximum level of retrieved linked content items.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async virtual Task<Article> GetCurrent(int maxLevel = 1, CancellationToken? cancellationToken = null)
        {
            var page = pageDataContextRetriever.Retrieve<Article>().Page;

            return await repositoryCacheHelper.CacheData(
                async cancellationToken => await page.WithLinkedItems(maxLevel, cancellationToken),
                cancellationToken ?? CancellationToken.None,
                $"{nameof(ArticleRepository)}|{nameof(GetCurrent)}|WithLinkedItems|{page.NodeID}",
                () => page.GetLinkedItemsCacheDependencyKeys(maxLevel, cancellationToken)
            );
        }
    }
}