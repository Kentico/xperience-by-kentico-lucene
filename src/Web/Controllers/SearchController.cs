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
        public IActionResult Index(string query, int pageSize = 10, int page = 1, string facet = null, string sortBy = null)
        {
            var results = searchService.GlobalSearch(query, pageSize, page, facet, sortBy);

            return new JsonResult(results);
        }
    }
}
