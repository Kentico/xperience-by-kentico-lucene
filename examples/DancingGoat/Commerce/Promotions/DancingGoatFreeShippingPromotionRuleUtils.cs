using System;
using System.Linq;

namespace DancingGoat.Commerce;

/// <summary>
/// Utility methods and scope constants shared by the DancingGoat free shipping promotion rules.
/// </summary>
public static class DancingGoatFreeShippingPromotionRuleUtils
{
    /// <summary>
    /// Scope value that counts every cart item toward the requirement.
    /// </summary>
    /// <remarks>
    /// When the promotion rule's scope is set to this value, the requirement counts the whole cart. The
    /// promotion records no product trigger.
    /// </remarks>
    public const string SCOPE_ANY_PRODUCT = "any";

    /// <summary>
    /// Scope value for category-based requirements.
    /// </summary>
    /// <remarks>
    /// When the promotion rule's scope is set to this value, the requirement counts only the cart items of
    /// products that belong to any of the specified categories.
    /// </remarks>
    public const string SCOPE_CATEGORIES = "categories";

    /// <summary>
    /// Scope value for product-based requirements.
    /// </summary>
    /// <remarks>
    /// When the promotion rule's scope is set to this value, the requirement counts only the cart items of the
    /// specifically selected products.
    /// </remarks>
    public const string SCOPE_PRODUCTS = "products";

    /// <summary>
    /// Scope value for tag-based requirements.
    /// </summary>
    /// <remarks>
    /// When the promotion rule's scope is set to this value, the requirement counts only the cart items of
    /// products that have any of the specified tags.
    /// </remarks>
    public const string SCOPE_TAGS = "tags";


    /// <summary>
    /// Determines whether the given product is in the promotion's scope.
    /// </summary>
    /// <param name="properties">Properties holding the scope configuration to evaluate against.</param>
    /// <param name="productData">Product data to check.</param>
    /// <returns><c>true</c> if the product is in the scope, otherwise <c>false</c>.</returns>
    public static bool IsInScope(DancingGoatFreeShippingProperties properties, DancingGoatProductData productData)
    {
        return properties.Scope.Equals(SCOPE_ANY_PRODUCT, StringComparison.InvariantCultureIgnoreCase)
            || properties.Scope.Equals(SCOPE_CATEGORIES, StringComparison.InvariantCultureIgnoreCase) && productData.Categories.Intersect(properties.ProductCategories).Any()
            || properties.Scope.Equals(SCOPE_TAGS, StringComparison.InvariantCultureIgnoreCase) && productData.Tags.Intersect(properties.ProductTags).Any()
            || properties.Scope.Equals(SCOPE_PRODUCTS, StringComparison.InvariantCultureIgnoreCase) && properties.Products.Select(product => product.Identifier).Contains(productData.ContentItemGuid);
    }
}
