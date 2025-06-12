using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CMS.ContentEngine;
using CMS.Websites;

using Kentico.Content.Web.Mvc;

namespace DancingGoat.Models
{
    /// <summary>
    /// Repository for managing product-related data retrieval operations.
    /// </summary>
    public class ProductRepository
    {
        private readonly IContentRetriever contentRetriever;


        /// <summary>
        /// Initializes a new instance of the <see cref="ProductRepository"/> class.
        /// </summary>
        /// <param name="contentRetriever">The content retriever.</param>
        public ProductRepository(IContentRetriever contentRetriever)
        {
            this.contentRetriever = contentRetriever;
        }


        /// <summary>
        /// Retrieves products by their content item IDs.
        /// </summary>
        /// <param name="productIds">The collection of product content item IDs to retrieve.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async Task<IEnumerable<IProductFields>> GetProductsByIds(IEnumerable<int> productIds, CancellationToken cancellationToken = default)
        {
            var products = await contentRetriever.RetrieveContentOfReusableSchemas<IProductFields>(
                [IProductFields.REUSABLE_FIELD_SCHEMA_NAME],
                new RetrieveContentOfReusableSchemasParameters
                {
                    LinkedItemsMaxLevel = 1,
                    WorkspaceNames = [DancingGoatConstants.COMMERCE_WORKSPACE_NAME]
                },
                query => query.Where(where => where.WhereIn(nameof(IContentQueryDataContainer.ContentItemID), productIds)),
                new RetrievalCacheSettings($"WhereIn_{nameof(IContentQueryDataContainer.ContentItemID)}_{string.Join("_", productIds)}"),
                cancellationToken
            );

            return products;
        }


        /// <summary>
        /// Retrieves the URLs of product pages associated with the specified product IDs.
        /// </summary>
        /// <param name="productIds">The collection of product content item IDs for which to retrieve page URLs.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async Task<Dictionary<int, string>> GetProductPageUrls(IEnumerable<int> productIds, CancellationToken cancellationToken = default)
        {
            var productPages = await contentRetriever.RetrievePages<ProductPage>(
                new RetrievePagesParameters
                {
                    LinkedItemsMaxLevel = 1,
                    PathMatch = PathMatch.Children(DancingGoatConstants.PRODUCTS_PAGE_TREE_PATH)
                },
                query => query.Linking(nameof(ProductPage.ProductPageProduct), productIds),
                new RetrievalCacheSettings($"Linking_{nameof(ProductPage.ProductPageProduct)}_{string.Join("_", productIds)}"),
                cancellationToken
            );

            var productPageUrls = productPages.ToDictionary(
                p => p.ProductPageProduct.First().SystemFields.ContentItemID,
                p => p.GetUrl().RelativePath
            );

            return productPageUrls;
        }
    }
}
