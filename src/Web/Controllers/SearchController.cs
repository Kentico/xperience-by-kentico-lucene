using DancingGoat.Search;
using Microsoft.AspNetCore.Mvc;

namespace DancingGoat.Controllers
{
    public class SearchController : Controller
    {
        private readonly SearchService searchService;

        public SearchController(SearchService searchService)
        { 
            this.searchService = searchService;
        }

        [Route("search")]
        public IActionResult Index(string query, int pageSize = 10, int page = 1)
        {
            var results = searchService.GlobalSearch(query, pageSize, page);

            return new JsonResult(results);
        }
    }
}
