using System.Collections.Generic;

using DancingGoat.Models;

using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace DancingGoat.Widgets
{
    /// <summary>
    /// Product card widget filter for content item selector.
    /// </summary>
    public class ProductCardSchemaFilter : IReusableFieldSchemasFilter
    {
        /// <inheritdoc/>
        IEnumerable<string> IReusableFieldSchemasFilter.AllowedSchemaNames => new List<string> { IProductFields.REUSABLE_FIELD_SCHEMA_NAME };
    }
}
