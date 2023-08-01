using Microsoft.AspNetCore.Mvc;

namespace DancingGoat.Search;

public class SearchController : Controller
{
    private readonly DancingGoatSearchService searchService;

    public SearchController(DancingGoatSearchService searchService) => this.searchService = searchService;

    [HttpGet]
    public IActionResult Index(string query, int pageSize = 10, int page = 1)
    {
        var results = searchService.Search(query, pageSize, page);

        return View(results);
    }
}

public record SearchRequest(string Query = "", int PageSize = 20, int Page = 1);
