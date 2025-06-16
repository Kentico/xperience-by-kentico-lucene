using System.Linq;
using System.Threading.Tasks;

using CMS.ContentEngine;

using DancingGoat.Models;

namespace DancingGoat.Commerce;

/// <summary>
/// Provides functionality to validate product SKU codes against existing content items.  
/// This class ensures that SKU codes are unique across published and draft versions of content items.  
/// </summary>
internal sealed class ProductSkuValidator
{
    private readonly IContentQueryExecutor executor;


    public ProductSkuValidator(IContentQueryExecutor executor)
    {
        this.executor = executor;
    }


    /// <summary>
    /// Checks if the provided SKU code is already used by another content item.    
    /// </summary>
    /// <param name="skuCode">The SKU code to check.</param>
    /// <param name="contentItemId">The ID of the content item to exclude from the check. This is used when updating an existing content item.</param>  
    /// <returns>The identifier of the duplicate content item or null if no duplicates were found.</returns>   
    public async Task<int?> GetCollidingContentItem(string skuCode, int? contentItemId)
    {
        var queryBuilder = new ContentItemQueryBuilder()
            .ForContentTypes(ct => ct.OfReusableSchema(IProductSKU.REUSABLE_FIELD_SCHEMA_NAME))
            .Parameters(p =>
                p.Where(w => w.WhereEquals(nameof(IProductSKU.ProductSKUCode), skuCode))
            );

        // Exclude the current content item from the query if it is provided
        if (contentItemId != null)
        {
            queryBuilder.Parameters(p =>
                p.Where(w => w.WhereNotEquals(nameof(IContentItemFieldsSource.SystemFields.ContentItemID), contentItemId))
            );
        }

        // Searches for product SKUs in the published versions of products
        var publishedDuplicateProducts = await executor.GetResult<int?>(queryBuilder,
            rowData => rowData.ContentItemID,
            new ContentQueryExecutionOptions { ForPreview = false });

        // Searches for product SKUs in the draft versions of products
        queryBuilder.Parameters(p =>
            p.Where(w => w.WhereIn(nameof(IContentItemFieldsSource.SystemFields.ContentItemCommonDataVersionStatus), [(int)VersionStatus.InitialDraft, (int)VersionStatus.Draft]))
        );

        var draftDuplicate = await executor.GetResult<int?>(queryBuilder,
            rowData => rowData.ContentItemID,
            new ContentQueryExecutionOptions { ForPreview = true });

        return publishedDuplicateProducts.FirstOrDefault() ?? draftDuplicate.FirstOrDefault();
    }
}
