#nullable enable

using Kentico.Xperience.Lucene.Core.Search;

namespace DancingGoat.Search;

public sealed record DancingGoatSearchFacetOptionViewModel(string Value, string Label, int Count);

public sealed record DancingGoatSearchFacetGroupViewModel(
    string QueryParameter,
    string Label,
    string? SelectedValue,
    IReadOnlyList<DancingGoatSearchFacetOptionViewModel> Options);

public sealed record DancingGoatSearchFilters(
    string? ProductCategory,
    string? ProductTag,
    string? ProductManufacturer,
    string? CoffeeProcessing,
    string? CoffeeTaste);

public sealed record DancingGoatSearchResponse(
    LuceneSearchResultModel<DancingGoatSearchResultModel> Results,
    IReadOnlyList<DancingGoatSearchFacetGroupViewModel> FacetGroups,
    DancingGoatSearchFilters Filters);

public sealed class DancingGoatSearchPageViewModel(DancingGoatSearchResponse response, string? sortBy)
{
    public LuceneSearchResultModel<DancingGoatSearchResultModel> Results { get; } = response.Results;
    public IReadOnlyList<DancingGoatSearchFacetGroupViewModel> FacetGroups { get; } = response.FacetGroups;
    public DancingGoatSearchFilters Filters { get; } = response.Filters;
    public string? SortBy { get; } = sortBy;
}
