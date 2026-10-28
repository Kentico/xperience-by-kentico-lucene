#nullable enable

using System.Net;

using CMS.ContentEngine;

using Kentico.Xperience.Lucene.Core;
using Kentico.Xperience.Lucene.Core.Indexing;
using Kentico.Xperience.Lucene.Core.Search;

using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Facet;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Highlight;
using Lucene.Net.Util;

namespace DancingGoat.Search;

public sealed class DancingGoatSearchService(
    ILuceneSearchService luceneSearchService,
    ILuceneIndexManager luceneIndexManager,
    DancingGoatSearchIndexingStrategy strategy,
    ITaxonomyRetriever taxonomyRetriever)
{
    public const string INDEX_NAME = "DancingGoatGlobal";

    private const int MAX_RESULTS = 1000;
    private const int MAX_PAGE_SIZE = 50;
    private const int MAX_FACET_VALUES = 50;
    private const int PHRASE_SLOP = 3;
    private const int SNIPPET_LENGTH = 240;

    private static readonly TaxonomyFacetDefinition[] TAXONOMY_FACETS =
    [
        new(
            DancingGoatSearchIndexingStrategy.PRODUCT_CATEGORY_FACET_DIMENSION,
            "productCategory",
            "Product categories",
            DancingGoatTaxonomyConstants.PRODUCT_CATEGORIES_TAXONOMY_NAME,
            filters => filters.ProductCategory),
        new(
            DancingGoatSearchIndexingStrategy.PRODUCT_TAG_FACET_DIMENSION,
            "productTag",
            "Product tags",
            DancingGoatTaxonomyConstants.PRODUCT_TAGS_TAXONOMY_NAME,
            filters => filters.ProductTag),
        new(
            DancingGoatSearchIndexingStrategy.PRODUCT_MANUFACTURER_FACET_DIMENSION,
            "productManufacturer",
            "Product manufacturers",
            DancingGoatTaxonomyConstants.PRODUCT_MANUFACTURERS_TAXONOMY_NAME,
            filters => filters.ProductManufacturer),
        new(
            DancingGoatSearchIndexingStrategy.COFFEE_PROCESSING_FACET_DIMENSION,
            "coffeeProcessing",
            "Coffee processing",
            "CoffeeProcessing",
            filters => filters.CoffeeProcessing),
        new(
            DancingGoatSearchIndexingStrategy.COFFEE_TASTE_FACET_DIMENSION,
            "coffeeTaste",
            "Coffee tastes",
            "CoffeeTastes",
            filters => filters.CoffeeTaste)
    ];

    private readonly ILuceneSearchService luceneSearchService = luceneSearchService;
    private readonly ILuceneIndexManager luceneIndexManager = luceneIndexManager;
    private readonly DancingGoatSearchIndexingStrategy strategy = strategy;
    private readonly ITaxonomyRetriever taxonomyRetriever = taxonomyRetriever;

    public async Task<DancingGoatSearchResponse> Search(
        string? searchText,
        int pageSize = 10,
        int page = 1,
        string? facet = null,
        string? sortBy = null,
        DancingGoatSearchFilters? filters = null,
        string languageName = "en",
        CancellationToken cancellationToken = default)
    {
        filters ??= new DancingGoatSearchFilters(null, null, null, null, null);

        var taxonomyLabels = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
        foreach (var definition in TAXONOMY_FACETS)
        {
            var taxonomy = await taxonomyRetriever.RetrieveTaxonomy(
                definition.TaxonomyName,
                languageName,
                cancellationToken);
            taxonomyLabels[definition.Dimension] = taxonomy.Tags.ToDictionary(
                tag => tag.Identifier.ToString("D"),
                tag => string.IsNullOrWhiteSpace(tag.Title) ? tag.Name : tag.Title,
                StringComparer.OrdinalIgnoreCase);
        }

        var index = luceneIndexManager.GetRequiredIndex(INDEX_NAME);
        var textQuery = GetTextQuery(searchText, index);
        var query = new BooleanQuery
        {
            { textQuery, Occur.MUST },
            { new TermQuery(new Term(BaseDocumentProperties.LANGUAGE_NAME, languageName)) { Boost = 0 }, Occur.MUST }
        };
        var filteredQuery = GetFilteredQuery(query, facet, filters);

        return luceneSearchService.UseSearcherWithFacets(
            index,
            query,
            MAX_RESULTS,
            (searcher, facets) =>
            {
                var topDocs = GetSortField(sortBy) is { } sortField
                    ? searcher.Search(filteredQuery, MAX_RESULTS, new Sort(sortField))
                    : searcher.Search(filteredQuery, MAX_RESULTS);

                pageSize = Math.Clamp(pageSize, 1, MAX_PAGE_SIZE);
                var totalHits = Math.Min(topDocs.TotalHits, MAX_RESULTS);
                var totalPages = (int)Math.Ceiling((double)totalHits / pageSize);
                page = totalPages == 0 ? 1 : Math.Clamp(page, 1, totalPages);

                var facetValues = facets.GetTopChildren(10, DancingGoatSearchIndexingStrategy.FACET_DIMENSION)?.LabelValues ?? [];
                var facetGroups = TAXONOMY_FACETS
                    .Where(definition =>
                        !string.Equals(facet, "Article", StringComparison.OrdinalIgnoreCase)
                        || !string.IsNullOrWhiteSpace(definition.GetSelectedValue(filters)))
                    .Select(definition =>
                    {
                        var options = facets.GetTopChildren(MAX_FACET_VALUES, definition.Dimension)?.LabelValues ?? [];
                        var labels = taxonomyLabels[definition.Dimension];
                        var facetOptions = options
                            .Select(option => new DancingGoatSearchFacetOptionViewModel(
                                option.Label,
                                labels.TryGetValue(option.Label, out var label) ? label : option.Label,
                                (int)option.Value))
                            .ToArray();

                        return new DancingGoatSearchFacetGroupViewModel(
                            definition.QueryParameter,
                            definition.Label,
                            definition.GetSelectedValue(filters),
                            facetOptions);
                    })
                    .ToArray();

                var highlighter = CreateHighlighter(textQuery);
                var results = new LuceneSearchResultModel<DancingGoatSearchResultModel>
                {
                    Query = searchText ?? string.Empty,
                    Page = page,
                    PageSize = pageSize,
                    TotalHits = totalHits,
                    TotalPages = totalPages,
                    Facet = facet,
                    Facets = facetValues,
                    Hits = topDocs.ScoreDocs
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .Select(hit => MapResult(searcher.Doc(hit.Doc), highlighter, index.LuceneAnalyzer))
                        .ToArray()
                };

                return new DancingGoatSearchResponse(results, facetGroups, filters);
            });
    }

    private Query GetTextQuery(string? searchText, LuceneIndex index)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return new MatchAllDocsQuery();
        }

        var queryBuilder = new QueryBuilder(index.LuceneAnalyzer);
        var query = new BooleanQuery();
        AddQuery(query, queryBuilder.CreatePhraseQuery(nameof(DancingGoatSearchResultModel.Title), searchText, PHRASE_SLOP), 5);
        AddQuery(query, queryBuilder.CreatePhraseQuery(DancingGoatSearchIndexingStrategy.CONTENT_FIELD_NAME, searchText, PHRASE_SLOP), 1);
        AddQuery(query, queryBuilder.CreateBooleanQuery(nameof(DancingGoatSearchResultModel.Title), searchText, Occur.SHOULD), 0.5f);
        AddQuery(query, queryBuilder.CreateBooleanQuery(DancingGoatSearchIndexingStrategy.CONTENT_FIELD_NAME, searchText, Occur.SHOULD), 0.1f);

        return query.GetClauses().Length == 0 ? new MatchAllDocsQuery() : query;
    }

    private Query GetFilteredQuery(Query query, string? facet, DancingGoatSearchFilters filters)
    {
        var drillDownQuery = new DrillDownQuery(strategy.FacetsConfigFactory());
        var hasFilters = false;

        if (!string.IsNullOrWhiteSpace(facet))
        {
            drillDownQuery.Add(DancingGoatSearchIndexingStrategy.FACET_DIMENSION, facet);
            hasFilters = true;
        }

        foreach (var definition in TAXONOMY_FACETS)
        {
            var selectedValue = definition.GetSelectedValue(filters);
            if (!string.IsNullOrWhiteSpace(selectedValue))
            {
                drillDownQuery.Add(definition.Dimension, selectedValue);
                hasFilters = true;
            }
        }

        if (!hasFilters)
        {
            return query;
        }

        return new BooleanQuery
        {
            { query, Occur.MUST },
            { drillDownQuery, Occur.MUST }
        };
    }

    private static void AddQuery(BooleanQuery query, Query? textQueryPart, float boost)
    {
        if (textQueryPart is null)
        {
            return;
        }

        textQueryPart.Boost = boost;
        query.Add(textQueryPart, Occur.SHOULD);
    }

    private static SortField? GetSortField(string? sortBy) =>
        sortBy switch
        {
            "a-z" => new SortField(DancingGoatSearchIndexingStrategy.SORTABLE_TITLE_FIELD_NAME, SortFieldType.STRING),
            "z-a" => new SortField(DancingGoatSearchIndexingStrategy.SORTABLE_TITLE_FIELD_NAME, SortFieldType.STRING, true),
            _ => null
        };

    private static Highlighter? CreateHighlighter(Query textQuery)
    {
        if (textQuery is MatchAllDocsQuery)
        {
            return null;
        }

        var scorer = new QueryScorer(textQuery, DancingGoatSearchIndexingStrategy.CONTENT_FIELD_NAME);
        return new Highlighter(new SimpleHTMLFormatter("<mark>", "</mark>"), new SimpleHTMLEncoder(), scorer)
        {
            TextFragmenter = new SimpleSpanFragmenter(scorer, SNIPPET_LENGTH)
        };
    }

    private static DancingGoatSearchResultModel MapResult(Document document, Highlighter? highlighter, Analyzer analyzer)
    {
        var snippet = document.Get(nameof(DancingGoatSearchResultModel.Snippet)) ?? string.Empty;
        var content = document.Get(DancingGoatSearchIndexingStrategy.CONTENT_FIELD_NAME);
        var highlightedSnippet = highlighter is not null && !string.IsNullOrWhiteSpace(content)
            ? highlighter.GetBestFragment(analyzer, DancingGoatSearchIndexingStrategy.CONTENT_FIELD_NAME, content)
            : null;

        return new()
        {
            Title = document.Get(nameof(DancingGoatSearchResultModel.Title)),
            Snippet = snippet,
            SnippetHtml = string.IsNullOrWhiteSpace(highlightedSnippet) ? WebUtility.HtmlEncode(snippet) : highlightedSnippet.Trim(),
            Url = document.Get(nameof(DancingGoatSearchResultModel.Url)),
            ContentType = document.Get(nameof(DancingGoatSearchResultModel.ContentType))
        };
    }

    private sealed record TaxonomyFacetDefinition(
        string Dimension,
        string QueryParameter,
        string Label,
        string TaxonomyName,
        Func<DancingGoatSearchFilters, string?> GetSelectedValue);
}
