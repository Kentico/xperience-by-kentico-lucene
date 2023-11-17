using Microsoft.AspNetCore.Mvc;

namespace DancingGoat.Search;

public class CrawlerSearchController : Controller
{
    private readonly DancingGoatCrawlerSearchService searchService;

    public CrawlerSearchController(DancingGoatCrawlerSearchService searchService) => this.searchService = searchService;

    [HttpGet]
    public IActionResult Index(string query, int pageSize = 10, int page = 1)
    {
        var results = searchService.Search(query, pageSize, page);

        return View(results);
    }
}

