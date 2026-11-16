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
    private const int MAX_RESULTS = 1000;
    private const int MAX_PAGE_SIZE = 50;
    private const int MAX_FACET_VALUES = 50;
    private const int PHRASE_SLOP = 3;
    private const int SNIPPET_LENGTH = 240;
    private const string SNIPPET_ELLIPSIS = "…";

    private readonly ILuceneSearchService luceneSearchService = luceneSearchService;
    private readonly ILuceneIndexManager luceneIndexManager = luceneIndexManager;
    private readonly DancingGoatSearchIndexingStrategy strategy = strategy;
    private readonly ITaxonomyRetriever taxonomyRetriever = taxonomyRetriever;

    /// <param name="filters">Selected taxonomy tag identifiers keyed by <see cref="DancingGoatSearchFacetDefinition.QueryParameter"/>.</param>
    /// <param name="includeSecuredItems">Whether pages that require authentication are returned (the visitor is signed in).</param>
    public async Task<DancingGoatSearchResponse> Search(
        string? searchText,
        string languageName,
        IReadOnlyDictionary<string, string> filters,
        bool includeSecuredItems,
        int pageSize = 10,
        int page = 1,
        string? contentType = null,
        string? sortBy = null,
        CancellationToken cancellationToken = default)
    {
        var taxonomyLabels = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
        foreach (var definition in DancingGoatSearchFacets.TAXONOMY_FACETS)
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

        var index = luceneIndexManager.GetRequiredIndex(DancingGoatSearchIndexingStrategy.INDEX_NAME);
        var textQuery = GetTextQuery(searchText, index);
        var query = new BooleanQuery
        {
            { textQuery, Occur.MUST },
            { new TermQuery(new Term(BaseDocumentProperties.LANGUAGE_NAME, languageName)) { Boost = 0 }, Occur.MUST }
        };

        // Secured pages are in the index, so anonymous visitors must not see them in hits, snippets, or facet counts.
        if (!includeSecuredItems)
        {
            query.Add(
                new TermQuery(new Term(DancingGoatSearchIndexingStrategy.IS_SECURED_FIELD_NAME, DancingGoatSearchIndexingStrategy.NOT_SECURED_VALUE)) { Boost = 0 },
                Occur.MUST);
        }

        return luceneSearchService.UseSearcherWithDrillSideways(
            index,
            (searcher, drillSideways) =>
            {
                // Drill sideways counts each selected dimension as if it were not selected, so facet
                // options reflect every other active filter while the hits reflect all of them.
                var drillDownQuery = GetDrillDownQuery(query, contentType, filters);
                var drillSidewaysResult = drillSideways.Search(
                    drillDownQuery,
                    filter: null,
                    after: null,
                    topN: MAX_RESULTS,
                    sort: GetSort(sortBy),
                    doDocScores: false,
                    doMaxScore: false);
                var topDocs = drillSidewaysResult.Hits;
                var facets = drillSidewaysResult.Facets;

                pageSize = Math.Clamp(pageSize, 1, MAX_PAGE_SIZE);
                var totalHits = Math.Min(topDocs.TotalHits, MAX_RESULTS);
                var totalPages = (int)Math.Ceiling((double)totalHits / pageSize);
                page = totalPages == 0 ? 1 : Math.Clamp(page, 1, totalPages);

                // Groups without matching documents are hidden (e.g. product facets when only articles match),
                // unless a value is selected in them so the selection can still be removed.
                var facetGroups = DancingGoatSearchFacets.TAXONOMY_FACETS
                    .Select(definition => GetFacetGroup(definition, filters.GetValueOrDefault(definition.QueryParameter), facets, taxonomyLabels[definition.Dimension]))
                    .Where(group => group.Options.Count > 0 || !string.IsNullOrWhiteSpace(group.SelectedValue))
                    .ToArray();

                var highlighter = CreateHighlighter(textQuery);
                var results = new LuceneSearchResultModel<DancingGoatSearchResultModel>
                {
                    Query = searchText ?? string.Empty,
                    Page = page,
                    PageSize = pageSize,
                    TotalHits = totalHits,
                    TotalPages = totalPages,
                    Facet = contentType,
                    Hits = topDocs.ScoreDocs
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .Select(hit => MapResult(searcher.Doc(hit.Doc), highlighter, index.LuceneAnalyzer))
                        .ToArray()
                };

                return new DancingGoatSearchResponse(results, facetGroups);
            });
    }

    private static DancingGoatSearchFacetGroupViewModel GetFacetGroup(
        DancingGoatSearchFacetDefinition definition,
        string? selectedValue,
        Facets facets,
        IReadOnlyDictionary<string, string> labels)
    {
        var options = facets.GetTopChildren(MAX_FACET_VALUES, definition.Dimension)?.LabelValues ?? [];
        var facetOptions = options
            .Select(option => new DancingGoatSearchFacetOptionViewModel(
                option.Label,
                labels.GetValueOrDefault(option.Label, option.Label),
                (int)option.Value))
            .ToArray();

        return new DancingGoatSearchFacetGroupViewModel(
            definition.QueryParameter,
            definition.Label,
            selectedValue,
            definition.IsOpenByDefault,
            facetOptions);
    }

    private static Query GetTextQuery(string? searchText, LuceneIndex index)
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

    private DrillDownQuery GetDrillDownQuery(Query query, string? contentType, IReadOnlyDictionary<string, string> filters)
    {
        var drillDownQuery = new DrillDownQuery(strategy.FacetsConfigFactory(), query);

        if (!string.IsNullOrWhiteSpace(contentType))
        {
            drillDownQuery.Add(DancingGoatSearchFacets.CONTENT_TYPE_DIMENSION, contentType);
        }

        foreach (var definition in DancingGoatSearchFacets.TAXONOMY_FACETS)
        {
            if (filters.TryGetValue(definition.QueryParameter, out var selectedValue) && !string.IsNullOrWhiteSpace(selectedValue))
            {
                drillDownQuery.Add(definition.Dimension, selectedValue);
            }
        }

        return drillDownQuery;
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

    private static Sort? GetSort(string? sortBy) =>
        sortBy switch
        {
            "a-z" => new Sort(new SortField(DancingGoatSearchIndexingStrategy.SORTABLE_TITLE_FIELD_NAME, SortFieldType.STRING)),
            "z-a" => new Sort(new SortField(DancingGoatSearchIndexingStrategy.SORTABLE_TITLE_FIELD_NAME, SortFieldType.STRING, true)),
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
        var content = document.Get(DancingGoatSearchIndexingStrategy.CONTENT_FIELD_NAME) ?? string.Empty;
        string? highlightedSnippet = null;
        if (highlighter is not null && !string.IsNullOrWhiteSpace(content))
        {
            highlightedSnippet = highlighter.GetBestFragment(analyzer, DancingGoatSearchIndexingStrategy.CONTENT_FIELD_NAME, content);
        }

        var snippetHtml = string.IsNullOrWhiteSpace(highlightedSnippet)
            ? WebUtility.HtmlEncode(GetFallbackSnippet(content))
            : highlightedSnippet.Trim();

        return new()
        {
            Title = document.Get(nameof(DancingGoatSearchResultModel.Title)),
            SnippetHtml = snippetHtml,
            Url = document.Get(nameof(DancingGoatSearchResultModel.Url)),
            ContentType = document.Get(nameof(DancingGoatSearchResultModel.ContentType)),
            IsSecured = document.Get(DancingGoatSearchIndexingStrategy.IS_SECURED_FIELD_NAME) == DancingGoatSearchIndexingStrategy.SECURED_VALUE
        };
    }

    private static string GetFallbackSnippet(string content)
    {
        if (content.Length <= SNIPPET_LENGTH)
        {
            return content;
        }

        var lastSpace = content.LastIndexOf(' ', SNIPPET_LENGTH);
        var length = lastSpace > 0 ? lastSpace : SNIPPET_LENGTH;

        return content[..length].TrimEnd(' ', ',', ';', ':', '.') + SNIPPET_ELLIPSIS;
    }
}
