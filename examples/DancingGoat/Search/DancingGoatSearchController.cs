#nullable enable

using CMS.ContentEngine;
using CMS.DataEngine;

using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;

using Microsoft.AspNetCore.Mvc;

namespace DancingGoat.Search;

[Route($"{{{WebPageRoutingOptions.LANGUAGE_ROUTE_VALUE_KEY}}}/search")]
public sealed class DancingGoatSearchController(
    DancingGoatSearchService searchService,
    IPreferredLanguageRetriever preferredLanguageRetriever,
    IInfoProvider<ContentLanguageInfo> contentLanguageInfoProvider) : Controller
{
    public const string SEARCH_ROUTE_NAME = "DancingGoatSearch";

    [HttpGet("", Name = SEARCH_ROUTE_NAME)]
    [HttpGet("/search")]
    public async Task<IActionResult> Index(
        string? query,
        int pageSize = 10,
        int page = 1,
        string? facet = null,
        string? sortBy = null,
        string? productCategory = null,
        string? productTag = null,
        string? productManufacturer = null,
        string? coffeeProcessing = null,
        string? coffeeTaste = null,
        CancellationToken cancellationToken = default)
    {
        var language = await contentLanguageInfoProvider.GetAsync(preferredLanguageRetriever.Get(), cancellationToken);
        if (language is null)
        {
            return NotFound();
        }

        var filters = new DancingGoatSearchFilters(
            productCategory,
            productTag,
            productManufacturer,
            coffeeProcessing,
            coffeeTaste);
        var results = await searchService.Search(
            query,
            pageSize,
            page,
            facet,
            sortBy,
            filters,
            language.ContentLanguageName,
            cancellationToken);

        return View("~/Views/Search/Index.cshtml", new DancingGoatSearchPageViewModel(results, sortBy));
    }
}
