using System;
using System.Linq;

namespace DancingGoat.Commerce;

/// <summary>
/// Utility methods and scope constants shared by the DancingGoat cart item promotion rule.
/// </summary>
public static class DancingGoatCartItemPromotionRuleUtils
{
    /// <summary>
    /// Scope value for category-based promotions.
    /// </summary>
    /// <remarks>
    /// When the promotion rule's scope is set to this value, the promotion applies only to the cart items of
    /// products that belong to any of the specified categories.
    /// </remarks>
    public const string SCOPE_CATEGORIES = "categories";

    /// <summary>
    /// Scope value for product-based promotions.
    /// </summary>
    /// <remarks>
    /// When the promotion rule's scope is set to this value, the promotion applies only to the cart items of the
    /// specifically selected products.
    /// </remarks>
    public const string SCOPE_PRODUCTS = "products";

    /// <summary>
    /// Scope value for tag-based promotions.
    /// </summary>
    /// <remarks>
    /// When the promotion rule's scope is set to this value, the promotion applies only to the cart items of
    /// products that have any of the specified tags.
    /// </remarks>
    public const string SCOPE_TAGS = "tags";


    /// <summary>
    /// Determines whether the given product is in the promotion's scope.
    /// </summary>
    /// <param name="properties">Properties holding the scope configuration to evaluate against.</param>
    /// <param name="productData">Product data to check.</param>
    /// <returns><c>true</c> if the product is in the scope, otherwise <c>false</c>.</returns>
    public static bool IsInScope(DancingGoatCartItemPromotionRuleProperties properties, DancingGoatProductData productData)
    {
        return properties.Scope.Equals(SCOPE_CATEGORIES, StringComparison.InvariantCultureIgnoreCase) && productData.Categories.Intersect(properties.ProductCategories).Any()
            || properties.Scope.Equals(SCOPE_TAGS, StringComparison.InvariantCultureIgnoreCase) && productData.Tags.Intersect(properties.ProductTags).Any()
            || properties.Scope.Equals(SCOPE_PRODUCTS, StringComparison.InvariantCultureIgnoreCase) && properties.Products.Select(product => product.Identifier).Contains(productData.ContentItemGuid);
    }
}
