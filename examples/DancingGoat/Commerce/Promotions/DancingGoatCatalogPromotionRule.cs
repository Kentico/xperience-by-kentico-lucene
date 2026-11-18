using System;
using System.Linq;

using CMS.Commerce;

using DancingGoat.Commerce;

using Kentico.Xperience.Admin.DigitalCommerce;

[assembly: RegisterPromotionRule<DancingGoatCatalogPromotionRule>(DancingGoatCatalogPromotionRule.IDENTIFIER, PromotionType.Catalog, "{$dancinggoat.catalogpromotionrule.sample.name$}")]

namespace DancingGoat.Commerce;

/// <summary>
/// Represents a catalog promotion rule for DancingGoat demo site.
/// </summary>
/// <remarks>
/// This is a sample implementation demonstrating how to create a custom catalog promotion rule.
/// It applies promotions to products based on categories, tags, or specific product selection.
/// The discount is calculated using the base class's <see cref="CatalogPromotionRule{TPromotionRuleProperties, TProductIdentifier, TPriceCalculationRequest, TPriceCalculationResult}.GetDiscountAmount"/> method,
/// which supports both percentage and fixed amount discounts based on the promotion rule properties.
/// </remarks>
public sealed class DancingGoatCatalogPromotionRule : CatalogPromotionRule<DancingGoatCatalogPromotionRuleProperties, ProductVariantIdentifier, DancingGoatPriceCalculationRequest, DancingGoatPriceCalculationResult>
{
    /// <summary>
    /// Unique identifier for this promotion rule.
    /// </summary>
    public const string IDENTIFIER = "DancingGoatCatalogPromotionRule";

    /// <summary>
    /// Scope value for category-based promotions.
    /// </summary>
    /// <remarks>
    /// When the promotion rule's scope is set to this value, the promotion applies to products that belong to any of the specified categories.
    /// </remarks>
    public const string SCOPE_CATEGORIES = "categories";

    /// <summary>
    /// Scope value for product-based promotions.
    /// </summary>
    /// <remarks>
    /// When the promotion rule's scope is set to this value, the promotion applies only to the specifically selected products.
    /// </remarks>
    public const string SCOPE_PRODUCTS = "products";

    /// <summary>
    /// Scope value for tag-based promotions.
    /// </summary>
    /// <remarks>
    /// When the promotion rule's scope is set to this value, the promotion applies to products that have any of the specified tags.
    /// </remarks>
    public const string SCOPE_TAGS = "tags";


    /// <summary>
    /// Gets the promotion candidate that can be used for the specified product.
    /// </summary>
    /// <param name="identifier">Product identifier of a product to be considered for the promotion.</param>
    /// <param name="calculationData">Price calculation data containing product information and pricing details.</param>
    /// <returns>
    /// Promotion candidate with the unit price discount amount and display label if the promotion is applicable for the product,
    /// otherwise <c>null</c>.
    /// The promotion is applicable if the product matches the promotion rule's scope (categories, tags, or specific products).
    /// </returns>
    /// <remarks>
    /// The discount amount is calculated based on the product's unit price and the promotion rule's discount configuration (percentage or fixed amount).
    /// The method also includes a formatted discount label for display purposes.
    /// </remarks>
    public override CatalogPromotionCandidate GetPromotionCandidate(ProductVariantIdentifier identifier, IPriceCalculationData<DancingGoatPriceCalculationRequest, DancingGoatPriceCalculationResult> calculationData)
    {
        var productItem = calculationData.Result.Items.FirstOrDefault(
            item => item.ProductData is DancingGoatProductData productDataWithCategory && item.ProductIdentifier == identifier);

        if (productItem is null)
        {
            return null;
        }

        var productData = productItem.ProductData;

        var canApply =
            Properties.Scope.Equals(SCOPE_CATEGORIES, StringComparison.InvariantCultureIgnoreCase) && productData.Categories.Intersect(Properties.ProductCategories).Any()
            || Properties.Scope.Equals(SCOPE_TAGS, StringComparison.InvariantCultureIgnoreCase) && productData.Tags.Intersect(Properties.ProductTags).Any()
            || Properties.Scope.Equals(SCOPE_PRODUCTS, StringComparison.InvariantCultureIgnoreCase) && Properties.Products.Select(x => x.Identifier).Contains(productData.ContentItemGuid);

        if (canApply)
        {
            var discountAmount = GetDiscountAmount(productData.UnitPrice);

            return new DancingGoatCatalogPromotionCandidate()
            {
                UnitPriceDiscountAmount = discountAmount,
                PromotionDisplayLabel = GetDiscountValueLabel()
            };
        }

        return null;
    }
}
