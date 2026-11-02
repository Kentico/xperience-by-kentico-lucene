using System.Collections.Generic;

using DancingGoat.Models;

using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace DancingGoat.Commerce;

/// <summary>
/// Filter for product promotion schema selection.
/// Restricts content item selection to products using the <see cref="IProductFields"/> reusable field schema.
/// </summary>
public class ProductPromotionSchemaFilter : IReusableFieldSchemasFilter
{
    /// <inheritdoc/>
    IEnumerable<string> IReusableFieldSchemasFilter.AllowedSchemaNames => new List<string> { IProductFields.REUSABLE_FIELD_SCHEMA_NAME };
}
