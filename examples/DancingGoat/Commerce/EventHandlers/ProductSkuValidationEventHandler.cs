using System;
using System.Threading;
using System.Threading.Tasks;

using CMS.Base;
using CMS.ContentEngine;

using DancingGoat.Models;

namespace DancingGoat.Commerce;

/// <summary>
/// Validates that product SKU codes are unique when a product content item is created or updated.
/// </summary>
internal sealed class ProductSkuValidationEventHandler :
    IAsyncEventHandler<BeforeCreateContentItemEvent>,
    IAsyncEventHandler<BeforeUpdateDraftEvent>
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
    /// Validates SKU uniqueness before a content item is created.
    /// </summary>
    public Task HandleAsync(BeforeCreateContentItemEvent asyncEvent, CancellationToken cancellationToken)
        => ValidateUniqueSku(asyncEvent.Data.ContentItemData, asyncEvent.Data.ID, cancellationToken);


    /// <summary>
    /// Validates SKU uniqueness before an existing content item draft is updated.
    /// </summary>
    public Task HandleAsync(BeforeUpdateDraftEvent asyncEvent, CancellationToken cancellationToken)
        => ValidateUniqueSku(asyncEvent.Data.ContentItemData, asyncEvent.Data.ID, cancellationToken);


    private async Task ValidateUniqueSku(ContentItemData contentItemData, int? contentItemId, CancellationToken cancellationToken)
    {
        if (!contentItemData.TryGetValue<string>(nameof(IProductSKU.ProductSKUCode), out var skuCode))
        {
            return;
        }

        var duplicatedContentItemIdentifier = await productSkuValidator.GetCollidingContentItem(skuCode, contentItemId, cancellationToken);
        if (duplicatedContentItemIdentifier != null)
        {
            throw new InvalidOperationException($"The SKU code '{skuCode}' is already used by the content item '{duplicatedContentItemIdentifier}'.");
        }
    }
}
