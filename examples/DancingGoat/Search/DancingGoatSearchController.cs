#nullable enable

using DancingGoat;
using DancingGoat.Models;
using DancingGoat.Search;

using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;

using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWebPageRoute(SearchPage.CONTENT_TYPE_NAME, typeof(DancingGoatSearchController), WebsiteChannelNames = [DancingGoatConstants.WEBSITE_CHANNEL_NAME])]

namespace DancingGoat.Search;

/// <summary>
/// Handles requests for <see cref="SearchPage"/> pages. The page's language selects the indexed language,
/// and the search state (query, filters, sort, paging) is read from the query string.
/// </summary>
public sealed class DancingGoatSearchController(
    DancingGoatSearchService searchService,
    IPreferredLanguageRetriever preferredLanguageRetriever) : Controller
{
    public async Task<IActionResult> Index(
        string? query,
        int pageSize = 10,
        int page = 1,
        string? facet = null,
        string? sortBy = null,
        CancellationToken cancellationToken = default)
    {
        var filters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var definition in DancingGoatSearchFacets.TAXONOMY_FACETS)
        {
            var selectedValue = Request.Query[definition.QueryParameter].ToString();
            if (!string.IsNullOrWhiteSpace(selectedValue))
            {
                filters[definition.QueryParameter] = selectedValue;
            }
        }

        var results = await searchService.Search(
            query,
            preferredLanguageRetriever.Get(),
            filters,
            User.Identity?.IsAuthenticated == true,
            pageSize,
            page,
            facet,
            sortBy,
            cancellationToken);

        return new TemplateResult(new DancingGoatSearchPageViewModel(results, sortBy, Request.Path));
    }
}
