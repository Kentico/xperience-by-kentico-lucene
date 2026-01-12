using System;
using System.Threading.Tasks;

using CMS.ContentEngine;

using DancingGoat.Models;

namespace DancingGoat.Commerce;

/// <summary>
/// Handles events related to content items.
/// </summary>
internal sealed class ProductSkuValidationEventHandler
{
    private readonly ProductSkuValidator productSkuValidator;


    /// <summary>
    /// Initializes a new instance of the <see cref="ProductSkuValidationEventHandler"/> class.
    /// </summary>
    public ProductSkuValidationEventHandler(ProductSkuValidator productSkuValidator)
    {
        this.productSkuValidator = productSkuValidator;
    }

    /// <summary>
    /// WARNING: Using <c>.GetAwaiter().GetResult()</c> here is intentional.  
    /// <para>
    /// ContentItemEvents are currently synchronous, but our IContentQueryExecutor API is async only.
    /// We block here only because the event system does not support async handlers yet.  
    /// </para>
    /// <para>
    /// ⚠️ Do NOT use <c>.GetAwaiter().GetResult()</c> in hot paths (e.g. request pipeline, 
    /// controller actions, or performance-critical code). This can lead to thread pool
    /// starvation and severe performance issues in ASP.NET Core.  
    /// </para>
    /// </summary>
    public void Initialize()
    {
        ContentItemEvents.Create.Before += (sender, args) => ValidateUniqueSKU(args.ContentItemData, args.ID);
        ContentItemEvents.UpdateDraft.Before += (sender, args) => ValidateUniqueSKU(args.ContentItemData, args.ID);
    }


    /// <summary>
    /// Validates that the SKU code is unique for the given content item.
    /// </summary>
    /// <param name="contentItemData">The content item data to validate.</param>
    /// <param name="contentItemId">The ID of the content item being created or updated.</param>
    private void ValidateUniqueSKU(ContentItemData contentItemData, int? contentItemId)
    {
        if (contentItemData.TryGetValue<string>(nameof(IProductSKU.ProductSKUCode), out var skuCode))
        {
            int? duplicatedContentItemIdentifier = productSkuValidator.GetCollidingContentItem(skuCode, contentItemId).GetAwaiter().GetResult();

            if (duplicatedContentItemIdentifier != null)
            {
                throw new InvalidOperationException($"The SKU code '{skuCode}' is already used by the content item '{duplicatedContentItemIdentifier}'.");
            }
        }
    }
}
