using System;
using System.Collections.Generic;

using CMS.ContentEngine;

using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.DigitalCommerce;

namespace DancingGoat.Commerce;

/// <summary>
/// Properties for the DancingGoat purchase threshold free shipping promotion rule.
/// Extends base free shipping promotion rule properties with scope-based filtering options.
/// </summary>
public class DancingGoatFreeShippingProperties : FreeShippingPromotionRuleProperties
{
    /// <summary>
    /// Gets or sets the scope of the requirement (any product, categories, products, or tags).
    /// </summary>
    [DropDownComponent(
        Label = "{$dancinggoat.freeshippingpromotionrule.scope.label$}",
        Options = $"{DancingGoatFreeShippingPromotionRuleUtils.SCOPE_ANY_PRODUCT};{{$dancinggoat.freeshippingpromotionrule.scope.options.anyproduct$}}\n{DancingGoatFreeShippingPromotionRuleUtils.SCOPE_CATEGORIES};{{$dancinggoat.freeshippingpromotionrule.scope.options.categories$}}\n{DancingGoatFreeShippingPromotionRuleUtils.SCOPE_PRODUCTS};{{$dancinggoat.freeshippingpromotionrule.scope.options.products$}}\n{DancingGoatFreeShippingPromotionRuleUtils.SCOPE_TAGS};{{$dancinggoat.freeshippingpromotionrule.scope.options.tags$}}",
        Order = -70)]
    [RequiredValidationRule]
    public string Scope { get; set; } = DancingGoatFreeShippingPromotionRuleUtils.SCOPE_ANY_PRODUCT;


    /// <summary>
    /// Gets or sets the product categories that the requirement counts.
    /// Only used when <see cref="Scope"/> is set to <see cref="DancingGoatFreeShippingPromotionRuleUtils.SCOPE_CATEGORIES"/>.
    /// </summary>
    [TagSelectorComponent(
        "ProductCategories",
        Label = "{$dancinggoat.freeshippingpromotionrule.productcategories.label$}",
        MinSelectedTagsCount = 1,
        Order = -60)]
    [VisibleIfEqualTo(nameof(Scope), DancingGoatFreeShippingPromotionRuleUtils.SCOPE_CATEGORIES, StringComparison.InvariantCultureIgnoreCase)]
    [RequiredValidationRule]
    public IEnumerable<TagReference> ProductCategories { get; set; } = [];


    /// <summary>
    /// Gets or sets the products that the requirement counts.
    /// Only used when <see cref="Scope"/> is set to <see cref="DancingGoatFreeShippingPromotionRuleUtils.SCOPE_PRODUCTS"/>.
    /// </summary>
    [ContentItemSelectorComponent(
        typeof(ProductPromotionSchemaFilter),
        Label = "{$dancinggoat.freeshippingpromotionrule.products.label$}",
        MinimumItems = 1,
        AllowContentItemCreation = false,
        Order = -50)]
    [VisibleIfEqualTo(nameof(Scope), DancingGoatFreeShippingPromotionRuleUtils.SCOPE_PRODUCTS, StringComparison.InvariantCultureIgnoreCase)]
    [RequiredValidationRule]
    public IEnumerable<ContentItemReference> Products { get; set; } = [];


    /// <summary>
    /// Gets or sets the product tags that the requirement counts.
    /// Only used when <see cref="Scope"/> is set to <see cref="DancingGoatFreeShippingPromotionRuleUtils.SCOPE_TAGS"/>.
    /// </summary>
    [TagSelectorComponent(
        "ProductTags",
        Label = "{$dancinggoat.freeshippingpromotionrule.producttags.label$}",
        MinSelectedTagsCount = 1,
        Order = -40)]
    [VisibleIfEqualTo(nameof(Scope), DancingGoatFreeShippingPromotionRuleUtils.SCOPE_TAGS, StringComparison.InvariantCultureIgnoreCase)]
    [RequiredValidationRule]
    public IEnumerable<TagReference> ProductTags { get; set; } = [];
}
