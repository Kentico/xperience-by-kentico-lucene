using DancingGoat.Search.Services;
using Microsoft.AspNetCore.Mvc;

namespace DancingGoat.Search;

[Route("[controller]")]
[ApiController]
public class SearchController : Controller
{
    private readonly SimpleSearchService simpleSearchService;
    private readonly AdvancedSearchService advancedSearchService;

    public SearchController(SimpleSearchService simpleSearchService, AdvancedSearchService advancedSearchService)
    {
        this.simpleSearchService = simpleSearchService;
        this.advancedSearchService = advancedSearchService;
    }

    public IActionResult Index(string? query, int pageSize = 10, int page = 1, string? facet = null, string? sortBy = null, string? indexName = null)
    {
        try
        {
            var results = advancedSearchService.GlobalSearch(indexName ?? "Advanced", query, pageSize, page, facet, sortBy);
            return View(results);
        }
        catch
        {
            return NotFound();
        }
    }

    [HttpGet("Simple")]
    public IActionResult Simple(string? query, int pageSize = 10, int page = 1)
    {
        var results = simpleSearchService.GlobalSearch("Simple", query, pageSize, page);

        return Ok(results);
    }
}
