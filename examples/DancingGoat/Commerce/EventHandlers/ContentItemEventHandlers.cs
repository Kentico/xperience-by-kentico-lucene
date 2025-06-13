using System;
using System.Threading.Tasks;

using CMS.ContentEngine;

using DancingGoat.Models;

namespace DancingGoat.Commerce;

/// <summary>
/// Handles events related to content items.
/// </summary>
internal sealed class ContentItemEventHandlers
{
    private readonly ProductSkuValidator productSkuValidator;


    /// <summary>
    /// Initializes a new instance of the <see cref="ContentItemEventHandlers"/> class.
    /// </summary>
    public ContentItemEventHandlers(ProductSkuValidator productSkuValidator)
    {
        this.productSkuValidator = productSkuValidator;
    }


    public void Initialize()
    {
        ContentItemEvents.Create.Before += (sender, args) => ValidateUniqueSKU(args.ContentItemData, args.ID).GetAwaiter().GetResult();
        ContentItemEvents.UpdateDraft.Before += (sender, args) => ValidateUniqueSKU(args.ContentItemData, args.ID).GetAwaiter().GetResult();
    }


    /// <summary>
    /// Validates that the SKU code is unique for the given content item.
    /// </summary>
    /// <param name="contentItemData">The content item data to validate.</param>
    /// <param name="contentItemId">The ID of the content item being created or updated.</param>
    private async Task ValidateUniqueSKU(ContentItemData contentItemData, int? contentItemId)
    {
        if (contentItemData.TryGetValue<string>(nameof(IProductSKU.ProductSKUCode), out var skuCode))
        {
            int? duplicatedContentItemIdentifier = await productSkuValidator.GetCollidingContentItem(skuCode, contentItemId);

            if (duplicatedContentItemIdentifier != null)
            {
                throw new InvalidOperationException($"The SKU code '{skuCode}' is already used by the content item '{duplicatedContentItemIdentifier}'.");
            }
        }
    }
}
