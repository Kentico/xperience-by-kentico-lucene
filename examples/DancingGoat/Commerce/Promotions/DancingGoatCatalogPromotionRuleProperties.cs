using System;
using System.Collections.Generic;
using System.Linq;

using CMS.ContentEngine;

using Kentico.Forms.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.DigitalCommerce;

namespace DancingGoat.Commerce;

/// <summary>
/// Properties for DancingGoat catalog promotion rule.
/// Extends base catalog promotion rule properties with scope-based filtering options.
/// </summary>
public class DancingGoatCatalogPromotionRuleProperties : CatalogPromotionRuleProperties
{
    /// <summary>
    /// Gets or sets the scope of the promotion (categories, products, or tags).
    /// </summary>
    [DropDownComponent(
        Label = "{$dancinggoat.catalogpromotionrule.scope.label$}",
        Options = $"{DancingGoatCatalogPromotionRule.SCOPE_CATEGORIES};{{$dancinggoat.catalogpromotionrule.scope.options.categories$}}\n{DancingGoatCatalogPromotionRule.SCOPE_PRODUCTS};{{$dancinggoat.catalogpromotionrule.scope.options.products$}}\n{DancingGoatCatalogPromotionRule.SCOPE_TAGS};{{$dancinggoat.catalogpromotionrule.scope.options.tags$}}",
        Order = 1)]
    [RequiredValidationRule]
    public string Scope { get; set; }


    /// <summary>
    /// Gets or sets the product categories to which the promotion applies.
    /// Only used when <see cref="Scope"/> is set to <see cref="DancingGoatCatalogPromotionRule.SCOPE_CATEGORIES"/>.
    /// </summary>
    [TagSelectorComponent(
        "ProductCategories",
        Label = "{$dancinggoat.catalogpromotionrule.productcategories.label$}",
        MinSelectedTagsCount = 1,
        Order = 2)]
    [VisibleIfEqualTo(nameof(Scope), DancingGoatCatalogPromotionRule.SCOPE_CATEGORIES, StringComparison.InvariantCultureIgnoreCase)]
    [RequiredValidationRule]
    public IEnumerable<TagReference> ProductCategories { get; set; } = Enumerable.Empty<TagReference>();


    /// <summary>
    /// Gets or sets the specific products to which the promotion applies.
    /// Only used when <see cref="Scope"/> is set to <see cref="DancingGoatCatalogPromotionRule.SCOPE_PRODUCTS"/>.
    /// </summary>
    [ContentItemSelectorComponent(
        typeof(ProductPromotionSchemaFilter),
        Label = "{$dancinggoat.catalogpromotionrule.products.label$}",
        MinimumItems = 1,
        AllowContentItemCreation = false,
        Order = 3)]
    [VisibleIfEqualTo(nameof(Scope), DancingGoatCatalogPromotionRule.SCOPE_PRODUCTS, StringComparison.InvariantCultureIgnoreCase)]
    [RequiredValidationRule]
    public IEnumerable<ContentItemReference> Products { get; set; } = Enumerable.Empty<ContentItemReference>();


    /// <summary>
    /// Gets or sets the product tags to which the promotion applies.
    /// Only used when <see cref="Scope"/> is set to <see cref="DancingGoatCatalogPromotionRule.SCOPE_TAGS"/>.
    /// </summary>
    [TagSelectorComponent(
        "ProductTags",
        Label = "{$dancinggoat.catalogpromotionrule.producttags.label$}",
        MinSelectedTagsCount = 1,
        Order = 4)]
    [RequiredValidationRule]
    [VisibleIfEqualTo(nameof(Scope), DancingGoatCatalogPromotionRule.SCOPE_TAGS, StringComparison.InvariantCultureIgnoreCase)]
    public IEnumerable<TagReference> ProductTags { get; set; } = Enumerable.Empty<TagReference>();
}
