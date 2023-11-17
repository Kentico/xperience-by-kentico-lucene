using CMS.Core;
using Kentico.Xperience.Lucene;
using Kentico.Xperience.Lucene.Services;
using Microsoft.AspNetCore.Mvc;

namespace DancingGoat.Search;

public class CafeSearchController : Controller
{
    public record SearchRequest(string Query = "", int PageSize = 20, int Page = 1);
    public record RebuildSearchIndexRequest(string IndexName, string Secret);

    // replace with real secret loaded from config
    private const string REBUILD_SECRET = "1234567890aaabbbccc";
    private readonly CafeSearchService searchService;
    private readonly ILuceneClient luceneClient;
    private readonly IEventLogService eventLogService;

    public CafeSearchController(CafeSearchService searchService, ILuceneClient luceneClient, IEventLogService eventLogService)
    {
        this.searchService = searchService;
        this.luceneClient = luceneClient;
        this.eventLogService = eventLogService;
    }

    [HttpGet]
    public IActionResult Index(string query, int pageSize = 10, int page = 1, string facet = null)
    {
        var results = searchService.Search(query, pageSize, page, facet);

        return View(results);
    }


    /// <summary>
    /// Rebuild of index could be initialized by HTTP POST request to url [webroot]/search/rebuild with body 
    /// <code>
    /// { 
    ///     "indexName": "...",
    ///     "secret": "..."
    /// }
    /// </code>
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost]
    public async Task<IActionResult> Rebuild([FromBody] RebuildSearchIndexRequest request)
    {
        try
        {
            if (request.Secret != REBUILD_SECRET)
            {
                return Unauthorized("Invalid Secret");
            }

            if (string.IsNullOrWhiteSpace(request.IndexName))
            {
                return NotFound($"IndexName is required");
            }

            var index = IndexStore.Instance.GetIndex(CafeSearchModel.IndexName);
            if (index == null)
            {
                return NotFound($"Index not found: {request.IndexName}");
            }

            await luceneClient.Rebuild(index.IndexName, null);
            return Ok("Index rebuild started");
        }
        catch (Exception ex)
        {
            eventLogService.LogException(nameof(SearchController), nameof(Rebuild), ex, 0, $"IndexName: {request.IndexName}");
            return Problem("Index rebuild failed");
        }
    }
}

