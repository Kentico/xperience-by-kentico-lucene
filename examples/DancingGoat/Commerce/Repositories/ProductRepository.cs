using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CMS.ContentEngine;
using CMS.Websites;

using DancingGoat.Models;

using Kentico.Content.Web.Mvc;

namespace DancingGoat.Commerce;

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
    /// Retrieves products by their content item GUIDs.
    /// </summary>
    /// <param name="productGuids">The collection of product content item GUIDs to retrieve.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<IEnumerable<IProductFields>> GetProductsByGuids(IEnumerable<Guid> productGuids, CancellationToken cancellationToken = default)
    {
        var products = await contentRetriever.RetrieveContentOfReusableSchemas<IProductFields>(
            [IProductFields.REUSABLE_FIELD_SCHEMA_NAME],
            new RetrieveContentOfReusableSchemasParameters
            {
                LinkedItemsMaxLevel = 1,
                WorkspaceNames = [DancingGoatConstants.COMMERCE_WORKSPACE_NAME]
            },
            query => query.Where(where => where.WhereIn(nameof(IContentQueryDataContainer.ContentItemGUID), productGuids)),
            new RetrievalCacheSettings($"WhereIn_{nameof(IContentQueryDataContainer.ContentItemGUID)}_{string.Join("_", productGuids)}"),
            cancellationToken
        );

        return products;
    }


    /// <summary>
    /// Retrieves products whose specified tag field contains any of the given tags.
    /// </summary>
    /// <param name="tagFieldName">Name of the product field holding the tags.</param>
    /// <param name="tagCollection">Tags to filter by.</param>
    /// <param name="includeSecuredItems">Indicates whether secured products are included.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Products whose <paramref name="tagFieldName"/> field contains any of the tags in <paramref name="tagCollection"/>.</returns>
    public async Task<IEnumerable<IProductFields>> GetProductsByTags(string tagFieldName, TagCollection tagCollection, bool includeSecuredItems = false, CancellationToken cancellationToken = default)
    {
        var products = await contentRetriever.RetrieveContentOfReusableSchemas<IProductFields>(
            [IProductFields.REUSABLE_FIELD_SCHEMA_NAME],
            new RetrieveContentOfReusableSchemasParameters
            {
                LinkedItemsMaxLevel = 1,
                WorkspaceNames = [DancingGoatConstants.COMMERCE_WORKSPACE_NAME],
                IncludeSecuredItems = includeSecuredItems
            },
            query => query.Where(where => where.WhereContainsTags(tagFieldName, tagCollection)),
            new RetrievalCacheSettings($"WhereContainsTags_{tagFieldName}_{string.Join("_", tagCollection.TagIdentifiers)}_{includeSecuredItems}"),
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
        if (!productIds.Any())
        {
            return [];
        }

        var productPages = await contentRetriever.RetrievePages<ProductPage>(
            new RetrievePagesParameters
            {
                LinkedItemsMaxLevel = 1,
                PathMatch = PathMatch.Children(DancingGoatConstants.PRODUCTS_PAGE_TREE_PATH),
                IncludeSecuredItems = true,
            },
            query => query.Linking(nameof(ProductPage.ProductPageProduct), productIds),
            new RetrievalCacheSettings($"Linking_{nameof(ProductPage.ProductPageProduct)}_{string.Join("_", productIds)}"),
            cancellationToken
        );

        var productPageUrls = productPages
            .Select(p => (Page: p, Product: p.ProductPageProduct?.FirstOrDefault() as IContentItemFieldsSource))
            .Where(x => x.Product is not null)
            .ToDictionary(
                x => x.Product.SystemFields.ContentItemID,
                x => x.Page.GetUrl().RelativePath
            );

        return productPageUrls;
    }
}
