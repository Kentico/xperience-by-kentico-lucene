#nullable enable

using Kentico.Xperience.Lucene.Core.Search;

using Microsoft.AspNetCore.WebUtilities;

namespace DancingGoat.Search;

public sealed record DancingGoatSearchFacetOptionViewModel(string Value, string Label, int Count);

public sealed record DancingGoatSearchFacetGroupViewModel(
    string QueryParameter,
    string Label,
    string? SelectedValue,
    bool IsOpenByDefault,
    IReadOnlyList<DancingGoatSearchFacetOptionViewModel> Options);

public sealed record DancingGoatSearchResponse(
    LuceneSearchResultModel<DancingGoatSearchResultModel> Results,
    IReadOnlyList<DancingGoatSearchFacetGroupViewModel> FacetGroups);

/// <param name="response">The search results and facet groups.</param>
/// <param name="sortBy">The selected sort order.</param>
/// <param name="pagePath">The path of the current search page, used as the base of every search link.</param>
public sealed class DancingGoatSearchPageViewModel(DancingGoatSearchResponse response, string? sortBy, string pagePath)
{
    public LuceneSearchResultModel<DancingGoatSearchResultModel> Results { get; } = response.Results;
    public IReadOnlyList<DancingGoatSearchFacetGroupViewModel> FacetGroups { get; } = response.FacetGroups;
    public string? SortBy { get; } = sortBy;
    public string PagePath { get; } = pagePath;

    /// <summary>
    /// Creates a search page URL with the given query string parameters. Parameters with empty values are skipped.
    /// </summary>
    public string GetUrl(IEnumerable<KeyValuePair<string, object?>>? parameters = null)
    {
        var queryParameters = (parameters ?? [])
            .Where(parameter => !string.IsNullOrEmpty(parameter.Value?.ToString()))
            .Select(parameter => new KeyValuePair<string, string?>(parameter.Key, parameter.Value!.ToString()));

        return QueryHelpers.AddQueryString(PagePath, queryParameters);
    }
}
