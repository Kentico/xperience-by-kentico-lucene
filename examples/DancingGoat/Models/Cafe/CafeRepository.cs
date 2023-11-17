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
    /// Represents a collection of cafes.
    /// </summary>
    public partial class CafeRepository
    {
        private readonly IPageRetriever pageRetriever;
        private readonly RepositoryCacheHelper repositoryCacheHelper;


        /// <summary>
        /// Initializes a new instance of the <see cref="CafeRepository"/> class that returns cafes.
        /// </summary>
        /// <param name="pageRetriever">Retriever for pages based on given parameters.</param>
        /// <param name="repositoryCacheHelper">Cache helper for repositories making working with cache easier.</param>
        public CafeRepository(IPageRetriever pageRetriever, RepositoryCacheHelper repositoryCacheHelper)
        {
            this.pageRetriever = pageRetriever;
            this.repositoryCacheHelper = repositoryCacheHelper;
        }


        /// <summary>
        /// Returns an enumerable collection of company cafes ordered by a position in the content tree.
        /// </summary>
        /// <param name="nodeAliasPath">The node alias path of the articles section in the content tree.</param>
        /// <param name="count">The number of cafes to return. Use 0 as value to return all records.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task<IEnumerable<Cafe>> GetCompanyCafes(string nodeAliasPath, int count = 0, CancellationToken? cancellationToken = null)
        {
            const int maxLevel = 2;

            var cafes = pageRetriever.Retrieve<Cafe>(
                query => query
                    .Path(nodeAliasPath, PathTypeEnum.Children)
                    .TopN(count)
                    .WhereTrue("CafeIsCompanyCafe")
                    .OrderBy("NodeOrder"),
                cache => cache
                    .Key($"{nameof(CafeRepository)}|{nameof(GetCompanyCafes)}|{nodeAliasPath}|{count}")
                    // Include path dependency to flush cache when a new child page is created or page order is changed.
                    .Dependencies((_, builder) => builder.PagePath(nodeAliasPath, PathTypeEnum.Children).PageOrder()));


            return await repositoryCacheHelper.CacheData(async cancellationToken =>
            {
                var cafesWithLinkedItems = new List<Cafe>();

                foreach (var cafe in cafes)
                {
                    cafesWithLinkedItems.Add(await cafe.WithLinkedItems(maxLevel, cancellationToken));
                }

                return cafesWithLinkedItems;
            }, cancellationToken ?? CancellationToken.None,
               $"{nameof(CafeRepository)}|{nameof(GetCompanyCafes)}|WithLinkedItems|{nodeAliasPath}|{count}",
               () => cafes.GetLinkedItemsCacheDependencyKeys(2, cancellationToken));
        }


        /// <summary>
        /// Returns a single cafe for the given <paramref name="nodeId"/>.
        /// </summary>
        /// <param name="nodeId">Node ID.</param>
        public Cafe GetCafeByNodeId(int nodeId)
        {
            return pageRetriever.Retrieve<Cafe>(
                query => query
                    .WhereEquals("NodeID", nodeId)
                    .TopN(1),
                cache => cache
                    .Key($"{nameof(CafeRepository)}|{nameof(GetCafeByNodeId)}|{nodeId}"))
                .FirstOrDefault();
        }


        /// <summary>
        /// Returns an enumerable collection of partner cafes ordered by a position in the content tree.
        /// </summary>
        /// <param name="nodeAliasPath">The node alias path of the articles section in the content tree.</param>
        public IEnumerable<Cafe> GetPartnerCafes(string nodeAliasPath)
        {
            return pageRetriever.Retrieve<Cafe>(
              query => query
                  .Path(nodeAliasPath, PathTypeEnum.Children)
                  .WhereFalse("CafeIsCompanyCafe")
                  .OrderBy("NodeOrder"),
              cache => cache
                  .Key($"{nameof(CafeRepository)}|{nameof(GetPartnerCafes)}|{nodeAliasPath}")
                  // Include path dependency to flush cache when a new child page is created or page order is changed.
                  .Dependencies((_, builder) => builder.PagePath(nodeAliasPath, PathTypeEnum.Children).PageOrder()));
        }
    }
}