using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CMS.Commerce;
using CMS.ContentEngine;

using DancingGoat.Models;

namespace DancingGoat.Commerce;

/// <summary>
/// Provides product data retrieval logic by querying content items
/// from the underlying content management system (CMS).
///
/// This class implements <see cref="IProductDataRetriever{ProductVariantIdentifier, DancingGoatProductData}"/> for <see cref="DancingGoatProductData"/>
/// and is responsible for:
/// <list type="bullet">
///   <item><description>Building and executing content queries for product identifiers.</description></item>
///   <item><description>Mapping raw CMS content item data into <see cref="DancingGoatProductData"/> objects.</description></item>
///   <item><description>Returning results as a read-only dictionary keyed by product identifiers.</description></item>
/// </list>
/// </summary>
internal sealed class ProductDataRetriever<TProductIdentifier, TProductData> : IProductDataRetriever<TProductIdentifier, TProductData>
    where TProductIdentifier : ProductVariantIdentifier
    where TProductData : DancingGoatProductData
{
    private readonly IContentQueryExecutor contentQueryExecutor;
    private readonly ProductVariantsExtractor productVariantsExtractor;
    private readonly ProductNameProvider productNameProvider;


    /// <summary>
    /// Initializes a new instance of <see cref="ProductDataRetriever{TProductIdentifier, TProductData}"/>.
    /// </summary>
    public ProductDataRetriever(IContentQueryExecutor contentQueryExecutor, ProductVariantsExtractor productVariantsExtractor, ProductNameProvider productNameProvider)
    {
        this.contentQueryExecutor = contentQueryExecutor;
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
        // Query for standalone products (without variants) or parent products of product variants
        var productsBuilder = new ContentItemQueryBuilder()
            .ForContentTypes(configure => configure
                // Get the IProductFields reusable field schema representing standalone products (without variants) or the parent product of product variants
                .OfReusableSchema(IProductFields.REUSABLE_FIELD_SCHEMA_NAME)
                // Ensure that up to 1 level of linked items (product variants) are included for each parent product
                .WithLinkedItems(1)
            )
            .InLanguage(languageName)
            .Parameters(
                p => p.Where(
                    // ProductIdentifier.Identifier refers to either a standalone product or the parent of a product variant
                    w => w.WhereIn(nameof(ContentItemFields.ContentItemID), productIdentifiers.Select(x => x.Identifier))
                )
            );

        // Execute the query and get the result
        var productsResult = await contentQueryExecutor.GetMappedResult<IProductFields>(productsBuilder, cancellationToken: cancellationToken);

        var resultDictionary = new Dictionary<TProductIdentifier, TProductData>();

        foreach (var productIdentifier in productIdentifiers)
        {
            // Retrieve the standalone product or the parent product of a product variant
            var product = productsResult.FirstOrDefault(p => (p as IContentItemFieldsSource).SystemFields.ContentItemID == productIdentifier.Identifier);

            string skuCode = (product as IProductSKU)?.ProductSKUCode;

            // If the product identifier is a product variant, get the SKU code for the product variant
            if (productIdentifier.VariantIdentifier.HasValue)
            {
                skuCode = productVariantsExtractor.ExtractVariantsSKUCode(product)[productIdentifier.VariantIdentifier.Value];
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
