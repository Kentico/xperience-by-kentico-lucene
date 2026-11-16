using DancingGoat.Models;

namespace DancingGoat.Search;

/// <summary>
/// Facets shared by <see cref="DancingGoatSearchIndexingStrategy"/> and <see cref="DancingGoatSearchService"/>.
/// Add a taxonomy facet to <see cref="TAXONOMY_FACETS"/> and rebuild the index.
/// </summary>
public static class DancingGoatSearchFacets
{
    public const string CONTENT_TYPE_DIMENSION = "ContentType";
    public const string CONTENT_TYPE_QUERY_PARAMETER = "facet";
    public const string ARTICLE_CONTENT_TYPE = "Article";
    public const string PRODUCT_CONTENT_TYPE = "Product";

    public static readonly IReadOnlyList<DancingGoatSearchFacetDefinition> TAXONOMY_FACETS =
    [
        new(
            "ProductCategory",
            "productCategory",
            "Product categories",
            DancingGoatTaxonomyConstants.PRODUCT_CATEGORIES_TAXONOMY_NAME,
            source => (source as IProductFields)?.ProductFieldCategory,
            IsOpenByDefault: true),
        new(
            "ProductTag",
            "productTag",
            "Product tags",
            DancingGoatTaxonomyConstants.PRODUCT_TAGS_TAXONOMY_NAME,
            source => (source as IProductFields)?.ProductFieldTags),
        new(
            "ProductManufacturer",
            "productManufacturer",
            "Product manufacturers",
            DancingGoatTaxonomyConstants.PRODUCT_MANUFACTURERS_TAXONOMY_NAME,
            source => (source as IProductManufacturer)?.ProductManufacturerTag),
        new(
            "CoffeeProcessing",
            "coffeeProcessing",
            "Coffee processing",
            DancingGoatTaxonomyConstants.COFFEE_PROCESSING_TAXONOMY_NAME,
            source => (source as ProductCoffee)?.CoffeeProcessing),
        new(
            "CoffeeTaste",
            "coffeeTaste",
            "Coffee tastes",
            DancingGoatTaxonomyConstants.COFFEE_TASTES_TAXONOMY_NAME,
            source => (source as ProductCoffee)?.CoffeeTastes),
        new(
            "ArticleTopic",
            "articleTopic",
            "Article topics",
            DancingGoatTaxonomyConstants.ARTICLE_TOPICS_TAXONOMY_NAME,
            source => (source as ArticlePage)?.ArticlePageTopics,
            IsOpenByDefault: true)
    ];
}
