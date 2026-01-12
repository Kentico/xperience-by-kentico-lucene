using System;
using System.Collections.Generic;

using CMS.Commerce;
using CMS.ContentEngine;

namespace DancingGoat.Commerce;

/// <summary>
/// Product data extended with additional property of categories.
/// </summary>
public record DancingGoatProductData : ProductData
{
    /// <summary>
    /// Categories the product is assigned to.
    /// </summary>
    public IEnumerable<TagReference> Categories { get; init; }


    /// <summary>
    /// Tags of the product.
    /// </summary>
    public IEnumerable<TagReference> Tags { get; init; }


    /// <summary>
    /// Content item GUID.
    /// </summary>
    public Guid ContentItemGuid { get; init; }
}
