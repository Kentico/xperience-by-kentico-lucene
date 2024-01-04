using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using CMS.DocumentEngine;
using CMS.DocumentEngine.Types.DancingGoatCore;

using DancingGoat.Infrastructure;

using Kentico.Content.Web.Mvc;

namespace DancingGoat.Models
{
    /// <summary>
    /// Represents a collection of coffees.
    /// </summary>
    public partial class CoffeeRepository
    {
        private readonly IPageRetriever pageRetriever;
        private readonly IPageDataContextRetriever pageDataContextRetriever;
        private readonly RepositoryCacheHelper repositoryCacheHelper;


        /// <summary>
        /// Initializes a new instance of the <see cref="CafeRepository"/> class that returns coffees.
        /// </summary>
        /// <param name="pageRetriever">Retriever for pages based on given parameters.</param>
        /// <param name="repositoryCacheHelper">Cache helper for repositories making working with cache easier.</param>
        public CoffeeRepository(IPageRetriever pageRetriever, IPageDataContextRetriever pageDataContextRetriever, RepositoryCacheHelper repositoryCacheHelper)
        {
            this.pageRetriever = pageRetriever;
            this.pageDataContextRetriever = pageDataContextRetriever;
            this.repositoryCacheHelper = repositoryCacheHelper;
        }


        /// <summary>
        /// Returns an enumerable collection of coffees ordered by a position in the content tree.
        /// </summary>
        /// <param name="nodeAliasPath">The node alias path of the coffees section in the content tree.</param>
        /// <param name="count">The number of coffees to return. Use 0 as value to return all records.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task<IEnumerable<Coffee>> Get(string nodeAliasPath, int count = 0, CancellationToken? cancellationToken = null)
        {
            const int maxLevel = 2;

            var coffees = await pageRetriever.RetrieveAsync<Coffee>(
                query => query
                    .Path(nodeAliasPath, PathTypeEnum.Children)
                    .TopN(count)
                    .OrderBy("NodeOrder"),
                cache => cache
                    .Key($"{nameof(CoffeeRepository)}|{nameof(Get)}|{nodeAliasPath}|{count}")
                    // Include path dependency to flush cache when a new child page is created or page order is changed.
                    .Dependencies((_, builder) => builder.PagePath(nodeAliasPath, PathTypeEnum.Children).PageOrder())
                );

            return await repositoryCacheHelper.CacheData(async cancellationToken =>
            {
                var coffeeWithLinkedItems = new List<Coffee>();

                foreach (var coffee in coffees)
                {
                    coffeeWithLinkedItems.Add(await coffee.WithLinkedItems(maxLevel, cancellationToken));
                }

                return coffeeWithLinkedItems;
            }, cancellationToken ?? CancellationToken.None, 
               $"{nameof(CoffeeRepository)}|{nameof(Get)}|WithLinkedItems|{nodeAliasPath}|{count}",
               () => coffees.GetLinkedItemsCacheDependencyKeys(maxLevel, cancellationToken));

        }


        /// <summary>
        /// Returns an enumerable collection of coffees based on node guid.
        /// </summary>        
        /// <param name="nodeIds">Collection of the nodes identifiers.</param>
        public IEnumerable<Coffee> Get(ICollection<int> nodeIds)
        {
            return pageRetriever.Retrieve<Coffee>(
                query => query.WhereIn("NodeID", nodeIds),
                cache => cache
                    .Key($"{nameof(CoffeeRepository)}|{nameof(Get)}|{String.Join(";", nodeIds)}"));
        }


        /// <summary>
        /// Returns current coffee article.
        /// </summary>
        /// <param name="maxLevel">Maximum level of retrieved linked content items.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async virtual Task<Coffee> GetCurrent(int maxLevel = 1, CancellationToken? cancellationToken = null)
        {
            var page = pageDataContextRetriever.Retrieve<Coffee>().Page;

            return await repositoryCacheHelper.CacheData(
                async cancellationToken => await page.WithLinkedItems(maxLevel, cancellationToken),
                cancellationToken ?? CancellationToken.None,
                $"{nameof(CoffeeRepository)}|{nameof(GetCurrent)}|WithLinkedItems|{page.NodeID}",
                () => page.GetLinkedItemsCacheDependencyKeys(maxLevel, cancellationToken)
            );
        }
    }
}