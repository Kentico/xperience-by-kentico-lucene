using Microsoft.AspNetCore.Mvc;

namespace DancingGoat.Search;

public class CafeSearchController : Controller
{
    public record SearchRequest(string Query = "", int PageSize = 20, int Page = 1);
    public record RebuildSearchIndexRequest(string IndexName, string Secret);

    // replace with real secret loaded from config
    private readonly ArticleSearchService searchService;

    public CafeSearchController(ArticleSearchService searchService)
    {
        this.searchService = searchService;
    }

    [HttpGet]
    public IActionResult Index(string query, int pageSize = 10, int page = 1, string facet = null)
    {
        var results = searchService.Search(query, pageSize, page, facet);

        return View(results);
    }
}

