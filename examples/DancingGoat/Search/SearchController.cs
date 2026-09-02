using DancingGoat.Search.Services;

using Kentico.Xperience.Lucene.Core.Search;

using Microsoft.AspNetCore.Mvc;

namespace DancingGoat.Search;

[Route("[controller]")]
[ApiController]
public class SearchController(SimpleSearchService simpleSearchService, AdvancedSearchService advancedSearchService) : Controller
{
    private readonly SimpleSearchService simpleSearchService = simpleSearchService;
    private readonly AdvancedSearchService advancedSearchService = advancedSearchService;

    /// <summary>
    /// Visit /search to see the search UI
    /// </summary>
    /// <param name="query"></param>
    /// <param name="pageSize"></param>
    /// <param name="page"></param>
    /// <param name="facet"></param>
    /// <param name="sortBy"></param>
    /// <returns></returns>
    public IActionResult Index(string? query, int pageSize = 10, int page = 1, string? facet = null, string? sortBy = null)
    {
        try
        {
            var results = advancedSearchService.GlobalSearch("Advanced", query, pageSize, page, facet, sortBy);
            return View(results);
        }
        catch
        {
            var results = new LuceneSearchResultModel<DancingGoatSearchResultModel>
            {
                Facet = "",
                Facets = [],
                Hits = [],
                Page = page,
                PageSize = pageSize,
                Query = query,
                TotalHits = 0,
                TotalPages = 0,
            };
            return View(results);
        }
    }

    [HttpGet("Simple")]
    public IActionResult Simple(string? query, int pageSize = 10, int page = 1)
    {
        var results = simpleSearchService.GlobalSearch("Simple", query, pageSize, page);

        return Ok(results);
    }
}
