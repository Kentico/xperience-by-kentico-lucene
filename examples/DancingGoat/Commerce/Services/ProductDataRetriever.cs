using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CMS.Commerce;
using CMS.ContentEngine;

using DancingGoat.Models;

using Kentico.Content.Web.Mvc;

namespace DancingGoat.Commerce;

/// <summary>
/// Provides product data retrieval logic by querying content items
/// from the underlying content management system (CMS).
///
/// This class implements <see cref="IProductDataRetriever{ProductVariantIdentifier, DancingGoatProductData}"/> for <see cref="DancingGoatProductData"/>
/// and is responsible for:
/// <list type="bullet">
///   <item><description>Retrieving products for the given product identifiers with implicit caching.</description></item>
///   <item><description>Mapping raw CMS content item data into <see cref="DancingGoatProductData"/> objects.</description></item>
///   <item><description>Returning results as a read-only dictionary keyed by product identifiers.</description></item>
/// </list>
/// </summary>
internal sealed class ProductDataRetriever<TProductIdentifier, TProductData> : IProductDataRetriever<TProductIdentifier, TProductData>
    where TProductIdentifier : ProductVariantIdentifier
    where TProductData : DancingGoatProductData
{
    private readonly IContentRetriever contentRetriever;
    private readonly ProductVariantsExtractor productVariantsExtractor;
    private readonly ProductNameProvider productNameProvider;


    /// <summary>
    /// Initializes a new instance of <see cref="ProductDataRetriever{TProductIdentifier, TProductData}"/>.
    /// </summary>
    public ProductDataRetriever(IContentRetriever contentRetriever, ProductVariantsExtractor productVariantsExtractor, ProductNameProvider productNameProvider)
    {
        this.contentRetriever = contentRetriever;
        this.productVariantsExtractor = productVariantsExtractor;
        this.productNameProvider = productNameProvider;
    }


    /// <summary>
    /// Retrieves product data for the specified product identifiers.
    /// </summary>
    /// <param name="productIdentifiers">A collection of product identifiers to query.</param>
    /// <param name="languageName">The language name in which the product data should be retrieved.
    /// This is used to fetch localized content from the underlying content store.</param>
    /// <param name="cancellationToken">Token that can be used to cancel the asynchronous operation.</param>
    /// <returns>
    /// A read-only dictionary mapping product identifiers (<see cref="ProductVariantIdentifier"/>)
    /// to their corresponding <see cref="DancingGoatProductData"/>.
    /// </returns>
    public async Task<IReadOnlyDictionary<TProductIdentifier, TProductData>> Get(IEnumerable<TProductIdentifier> productIdentifiers, string languageName, CancellationToken cancellationToken)
    {
        var identifiers = productIdentifiers.ToList();

        var productIds = identifiers.Select(x => x.Identifier).ToList();

        var productsResult = await contentRetriever.RetrieveContentOfReusableSchemas<IProductFields>(
            [IProductFields.REUSABLE_FIELD_SCHEMA_NAME],
            new RetrieveContentOfReusableSchemasParameters
            {
                LinkedItemsMaxLevel = 1,
                LanguageName = languageName,
                WorkspaceNames = [DancingGoatConstants.COMMERCE_WORKSPACE_NAME]
            },
            query => query.Where(where => where.WhereIn(nameof(IContentQueryDataContainer.ContentItemID), productIds)),
            new RetrievalCacheSettings($"WhereIn_{nameof(IContentQueryDataContainer.ContentItemID)}_{string.Join("_", productIds)}"),
            cancellationToken
        );

        var resultDictionary = new Dictionary<TProductIdentifier, TProductData>();

        foreach (var productIdentifier in identifiers)
        {
            // Retrieve the standalone product or the parent product of a product variant
            var product = productsResult.FirstOrDefault(p => (p as IContentItemFieldsSource).SystemFields.ContentItemID == productIdentifier.Identifier);
            if (product == null)
            {
                // If the product is not found, continue to the next product identifier.
                // This can happen if the product is in shopping cart but not in the database.
                continue;
            }

            string skuCode = (product as IProductSKU)?.ProductSKUCode;

            // If the product identifier is a product variant, get the SKU code for the product variant
            if (productIdentifier.VariantIdentifier.HasValue)
            {
                // The extracted variants contain only published variants linked to the product,
                // so a variant that was unpublished, deleted or unlinked is skipped like a missing product.
                if (!productVariantsExtractor.ExtractVariantsSKUCode(product).TryGetValue(productIdentifier.VariantIdentifier.Value, out skuCode))
                {
                    continue;
                }
            }

            var productDataDto = new DancingGoatProductData
            {
                ProductName = productNameProvider.GetProductName(product, productIdentifier.VariantIdentifier),
                SKU = skuCode,
                UnitPrice = product.ProductFieldPrice,
                Categories = product.ProductFieldCategory,
                Tags = product.ProductFieldTags,
                ContentItemGuid = (product as IContentItemFieldsSource).SystemFields.ContentItemGUID
            };

            resultDictionary.Add(productIdentifier, (TProductData)productDataDto);
        }

        return resultDictionary.AsReadOnly();
    }
}
