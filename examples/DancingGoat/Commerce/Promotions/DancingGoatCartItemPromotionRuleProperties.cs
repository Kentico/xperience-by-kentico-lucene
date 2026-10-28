using System;
using System.Collections.Generic;

using CMS.Commerce;
using CMS.ContentEngine;

using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace DancingGoat.Commerce;

/// <summary>
/// Properties for DancingGoat cart item promotion rule.
/// Extends base promotion rule properties with scope-based filtering options.
/// </summary>
[FormCategory(Label = "{$dancinggoat.cartitempromotionrule.category.purchaserequirements$}", Order = 1)]
[FormCategory(Label = "{$dancinggoat.cartitempromotionrule.category.reward$}", Order = 7)]
public class DancingGoatCartItemPromotionRuleProperties : IPromotionRuleProperties
{
    /// <summary>
    /// Gets or sets the number of units in the promotion rule's scope required to discount one unit.
    /// The rule discounts the cheapest unit in every complete group of this size.
    /// </summary>
    [NumberInputComponent(
        Label = "{$dancinggoat.cartitempromotionrule.minimumquantity.label$}",
        Order = 2)]
    [MinimumIntegerValueValidationRule(2)]
    [RequiredValidationRule]
    public int MinimumQuantity { get; set; } = 3;


    /// <summary>
    /// Gets or sets how products eligible for the promotion are selected.
    /// </summary>
    [DropDownComponent(
        Label = "{$dancinggoat.cartitempromotionrule.scope.label$}",
        Options = $"{DancingGoatCartItemPromotionRuleUtils.SCOPE_CATEGORIES};{{$dancinggoat.cartitempromotionrule.scope.options.categories$}}\n{DancingGoatCartItemPromotionRuleUtils.SCOPE_PRODUCTS};{{$dancinggoat.cartitempromotionrule.scope.options.products$}}\n{DancingGoatCartItemPromotionRuleUtils.SCOPE_TAGS};{{$dancinggoat.cartitempromotionrule.scope.options.tags$}}",
        Order = 3)]
    [RequiredValidationRule]
    public string Scope { get; set; }


    /// <summary>
    /// Gets or sets the product categories that the promotion applies to.
    /// Only used when <see cref="Scope"/> is set to <see cref="DancingGoatCartItemPromotionRuleUtils.SCOPE_CATEGORIES"/>.
    /// </summary>
    [TagSelectorComponent(
        "ProductCategories",
        Label = "{$dancinggoat.cartitempromotionrule.productcategories.label$}",
        MinSelectedTagsCount = 1,
        Order = 4)]
    [VisibleIfEqualTo(nameof(Scope), DancingGoatCartItemPromotionRuleUtils.SCOPE_CATEGORIES, StringComparison.InvariantCultureIgnoreCase)]
    [RequiredValidationRule]
    public IEnumerable<TagReference> ProductCategories { get; set; } = [];


    /// <summary>
    /// Gets or sets the products that the promotion applies to.
    /// Only used when <see cref="Scope"/> is set to <see cref="DancingGoatCartItemPromotionRuleUtils.SCOPE_PRODUCTS"/>.
    /// </summary>
    [ContentItemSelectorComponent(
        typeof(ProductPromotionSchemaFilter),
        Label = "{$dancinggoat.cartitempromotionrule.products.label$}",
        MinimumItems = 1,
        AllowContentItemCreation = false,
        Order = 5)]
    [VisibleIfEqualTo(nameof(Scope), DancingGoatCartItemPromotionRuleUtils.SCOPE_PRODUCTS, StringComparison.InvariantCultureIgnoreCase)]
    [RequiredValidationRule]
    public IEnumerable<ContentItemReference> Products { get; set; } = [];


    /// <summary>
    /// Gets or sets the product tags that the promotion applies to.
    /// Only used when <see cref="Scope"/> is set to <see cref="DancingGoatCartItemPromotionRuleUtils.SCOPE_TAGS"/>.
    /// </summary>
    [TagSelectorComponent(
        "ProductTags",
        Label = "{$dancinggoat.cartitempromotionrule.producttags.label$}",
        MinSelectedTagsCount = 1,
        Order = 6)]
    [VisibleIfEqualTo(nameof(Scope), DancingGoatCartItemPromotionRuleUtils.SCOPE_TAGS, StringComparison.InvariantCultureIgnoreCase)]
    [RequiredValidationRule]
    public IEnumerable<TagReference> ProductTags { get; set; } = [];


    /// <summary>
    /// Gets or sets the discount percentage applied to each discounted unit.
    /// </summary>
    [DecimalNumberInputComponent(Label = "{$dancinggoat.cartitempromotionrule.discountpercentage.label$}", Order = 8)]
    [MinimumDecimalValueValidationRule(0)]
    [MaximumDecimalValueValidationRule(100)]
    [RequiredValidationRule]
    public decimal DiscountPercentage { get; set; } = 50;
}
