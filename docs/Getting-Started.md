# Getting Started with Lucene Search

This comprehensive guide walks you through setting up Lucene search in your Xperience by Kentico application from start to finish. By the end, you'll have a working search feature that indexes your content and displays search results to your users.

## Overview

Setting up Lucene search involves these main steps:

1. Install the NuGet package
2. Register services in your application
3. Create a custom indexing strategy
4. Configure the index in the admin UI
5. Create a search service to query the index
6. Build a search interface for your users

## Prerequisites

- An Xperience by Kentico application (version 28.0.0 or higher)
- Basic understanding of C# and ASP.NET Core
- Familiarity with Xperience content types and website channels

## Step 1: Install the NuGet Package

Add the Lucene package to your project using the .NET CLI:

```powershell
dotnet add package Kentico.Xperience.Lucene
```

This package includes both the admin UI components and the live site search functionality. For more information about the available packages, see the [README](../README.md).

## Step 2: Register Services

Open your `Program.cs` file and register the Lucene services. Start with the basic registration:

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// ... other service registrations

// Register Lucene services (without custom strategies yet)
builder.Services.AddKenticoLucene();

// ... rest of your code
```

This registers all necessary services with default behavior. We'll come back to add custom indexing strategies in Step 3.

## Step 3: Create a Custom Indexing Strategy

An indexing strategy defines how your content is transformed into searchable documents. Let's create one for a typical scenario - indexing article pages.

### 3.1: Define Your Search Result Model

First, create a model representing what data you want in search results:

```csharp
// Models/ArticleSearchResultModel.cs
namespace YourProject.Models
{
    public class ArticleSearchResultModel
    {
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public DateTime PublishedDate { get; set; }
        public string ContentType { get; set; } = string.Empty;
    }
}
```

### 3.2: Implement the Indexing Strategy

Create a class that inherits from `DefaultLuceneIndexingStrategy`:

```csharp
// Search/ArticleSearchIndexingStrategy.cs
using CMS.ContentEngine;
using CMS.Websites;
using Kentico.Xperience.Lucene.Core.Indexing;
using Kentico.Xperience.Lucene.Models;
using Lucene.Net.Documents;
using Lucene.Net.Facet;

namespace YourProject.Search
{
    public class ArticleSearchIndexingStrategy : DefaultLuceneIndexingStrategy
    {
        // Define custom field names
        public const string SUMMARY_FIELD = "Summary";
        public const string PUBLISHED_DATE_FIELD = "PublishedDate";
        
        private readonly IWebPageQueryResultMapper webPageMapper;
        private readonly IContentQueryExecutor queryExecutor;
        private readonly IWebPageUrlRetriever urlRetriever;

        public ArticleSearchIndexingStrategy(
            IWebPageQueryResultMapper webPageMapper,
            IContentQueryExecutor queryExecutor,
            IWebPageUrlRetriever urlRetriever
        )
        {
            this.webPageMapper = webPageMapper;
            this.queryExecutor = queryExecutor;
            this.urlRetriever = urlRetriever;
        }

        public override async Task<Document?> MapToLuceneDocumentOrNull(IIndexEventItemModel item)
        {
            // Only process web page items
            if (item is not IndexEventWebPageItemModel webPageItem)
            {
                return null;
            }

            // Only index ArticlePage content type (adjust to match your content type)
            if (!string.Equals(webPageItem.ContentTypeName, "ArticlePage", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            // Retrieve the full web page data
            var page = await GetPage<ArticlePage>(
                webPageItem.ItemGuid,
                webPageItem.WebsiteChannelName,
                webPageItem.LanguageName,
                "ArticlePage" // Use your actual content type name
            );

            if (page == null)
            {
                return null;
            }

            // Create a new Lucene document
            var document = new Document();

            // Add the title as a searchable and stored field
            string title = page.ArticleTitle ?? string.Empty;
            document.Add(new TextField(
                nameof(ArticleSearchResultModel.Title),
                title,
                Field.Store.YES
            ));

            // Add summary as a searchable and stored field
            string summary = page.ArticleSummary ?? string.Empty;
            document.Add(new TextField(
                SUMMARY_FIELD,
                summary,
                Field.Store.YES
            ));

            // Add published date as a stored string field for retrieval
            string publishedDate = page.ArticlePublishedDate.ToString("yyyy-MM-dd");
            document.Add(new StringField(
                PUBLISHED_DATE_FIELD,
                publishedDate,
                Field.Store.YES
            ));

            // Add the full article content for searching (but don't store it to save space)
            string content = page.ArticleText ?? string.Empty;
            document.Add(new TextField(
                "Content",
                content,
                Field.Store.NO
            ));

            // Retrieve and add the URL
            string url = string.Empty;
            try
            {
                url = (await urlRetriever.Retrieve(
                    webPageItem.WebPageItemTreePath,
                    webPageItem.WebsiteChannelName,
                    webPageItem.LanguageName
                )).RelativePath;
            }
            catch (Exception)
            {
                // Handle cases where URL retrieval fails
                // (e.g., page deleted before indexing completes)
            }

            document.Add(new StringField(
                nameof(ArticleSearchResultModel.Url),
                url,
                Field.Store.YES
            ));

            // Add content type as a facet for filtering
            document.Add(new FacetField(
                "ContentType",
                "Article"
            ));

            return document;
        }

        public override FacetsConfig FacetsConfigFactory()
        {
            var facetConfig = new FacetsConfig();
            facetConfig.SetMultiValued("ContentType", true);
            return facetConfig;
        }

        private async Task<T?> GetPage<T>(
            Guid id,
            string channelName,
            string languageName,
            string contentTypeName
        ) where T : IWebPageFieldsSource, new()
        {
            var query = new ContentItemQueryBuilder()
                .ForContentType(contentTypeName, config =>
                    config
                        .WithLinkedItems(4)
                        .ForWebsite(channelName)
                        .Where(where => where.WhereEquals(nameof(WebPageFields.WebPageItemGUID), id))
                        .TopN(1)
                )
                .InLanguage(languageName);

            var result = await queryExecutor.GetWebPageResult(query, webPageMapper.Map<T>);
            return result.FirstOrDefault();
        }
    }
}
```

### 3.3: Register Your Strategy

Update your `Program.cs` to register your custom strategy:

```csharp
// Program.cs
builder.Services.AddKenticoLucene(builder =>
{
    builder.RegisterStrategy<ArticleSearchIndexingStrategy>("ArticleStrategy");
});
```

The string "ArticleStrategy" is a unique identifier that will appear in the admin UI when configuring indexes.

## Step 4: Configure the Index in Admin UI

Now that you've defined how content should be indexed, configure an index in Xperience:

### 4.1: Access the Search Application

1. Log into your Xperience administration
2. Navigate to the **Search** application from the dashboard
3. Click **Create new** to create a new index

### 4.2: Configure Index Settings

Fill out the index configuration form:

- **Index Name**: Give your index a descriptive name (e.g., "Article Search")
- **Indexing Strategy**: Select "ArticleStrategy" (the one you registered)
- **Lucene Analyzer**: Choose "Standard" (or a language-specific analyzer if needed)
- **Indexed Languages**: Select the languages you want to index (e.g., "English")
- **Included Reusable Content Types**: Leave empty unless you're indexing reusable content items
- **Rebuild Hook**: (Optional) Enter a secret token if you plan to trigger rebuilds via API

Click **Save** to continue.

### 4.3: Configure Website Channel

After saving the index, configure which website channels and paths to include:

1. Click **Add New Website Channel Configuration**
2. Select your website channel from the dropdown
3. Click **Save**

### 4.4: Configure Included Paths

Now specify which pages in the channel should be indexed:

1. Click **Add New Path Configuration**
2. Configure the path:
   - **Included Path**: Enter a path like `/articles/%` to include all pages under the articles section
     - Use `%` as a wildcard to include all descendant pages
     - Or enter an exact path like `/articles/my-specific-article`
   - **Included Content Types**: Select "ArticlePage" (or your article content type)
3. Click **Save**

**Important**: The path should be the **Tree Path** from the page's properties, not the URL. To find the Tree Path:

1. Go to your website channel in the admin
2. Select a page in the content tree
3. View the page's properties to find the "Tree Path" value

### 4.5: Rebuild the Index

After configuration, rebuild the index to process all existing content:

1. Return to the Search application
2. Select your index from the list
3. Click the **Rebuild** button
4. Wait for the rebuild to complete (you'll see a success message)

## Step 5: Create a Search Service

Now create a service to query your index and return results:

```csharp
// Search/ArticleSearchService.cs
using Kentico.Xperience.Lucene.Core.Indexing;
using Kentico.Xperience.Lucene.Core.Search;
using Kentico.Xperience.Lucene.Models;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Util;
using YourProject.Models;

namespace YourProject.Search
{
    public class ArticleSearchService
    {
        private const int MAX_RESULTS = 1000;
        
        private readonly ILuceneSearchService luceneSearchService;
        private readonly ILuceneIndexManager luceneIndexManager;

        public ArticleSearchService(
            ILuceneSearchService luceneSearchService,
            ILuceneIndexManager luceneIndexManager
        )
        {
            this.luceneSearchService = luceneSearchService;
            this.luceneIndexManager = luceneIndexManager;
        }

        public LuceneSearchResultModel<ArticleSearchResultModel> Search(
            string indexName,
            string searchText,
            int pageSize = 10,
            int page = 1
        )
        {
            // Get the index
            var index = luceneIndexManager.GetRequiredIndex(indexName);
            
            // Build the query
            var query = CreateQuery(index, searchText);

            // Execute the search
            var result = luceneSearchService.UseSearcher(
                index,
                (searcher) =>
                {
                    // Perform the search
                    var topDocs = searcher.Search(query, MAX_RESULTS);
                    
                    // Calculate pagination
                    pageSize = Math.Max(1, pageSize);
                    page = Math.Max(1, page);
                    int offset = pageSize * (page - 1);
                    int limit = pageSize;

                    // Map documents to result models
                    var hits = topDocs.ScoreDocs
                        .Skip(offset)
                        .Take(limit)
                        .Select(scoreDoc => MapToResultItem(searcher.Doc(scoreDoc.Doc)))
                        .ToList();

                    return new LuceneSearchResultModel<ArticleSearchResultModel>
                    {
                        Query = searchText ?? string.Empty,
                        Page = page,
                        PageSize = pageSize,
                        TotalPages = topDocs.TotalHits <= 0 ? 0 : ((topDocs.TotalHits - 1) / pageSize) + 1,
                        TotalHits = topDocs.TotalHits,
                        Hits = hits
                    };
                }
            );

            return result;
        }

        private Query CreateQuery(LuceneIndex index, string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                // Return all documents if no search text
                return new MatchAllDocsQuery();
            }

            // Define fields and their boost values
            var fields = new[] {
                nameof(ArticleSearchResultModel.Title),
                ArticleSearchIndexingStrategy.SUMMARY_FIELD,
                "Content"
            };
            
            var boosts = new Dictionary<string, float>
            {
                { nameof(ArticleSearchResultModel.Title), 3.0f },
                { ArticleSearchIndexingStrategy.SUMMARY_FIELD, 2.0f },
                { "Content", 1.0f }
            };

            // Create a multi-field query parser with boost values
            var parser = new MultiFieldQueryParser(
                LuceneVersion.LUCENE_48,
                fields,
                index.Analyzer,
                boosts
            );

            parser.SetMultiTermRewriteMethod(MultiTermQuery.SCORING_BOOLEAN_REWRITE);

            // Parse the search text into a query
            try
            {
                var parsedQuery = parser.Parse(searchText);
                return parsedQuery;
            }
            catch (ParseException)
            {
                // If parsing fails, escape the text and try again
                var escapedText = QueryParser.Escape(searchText);
                return parser.Parse(escapedText);
            }
        }

        private ArticleSearchResultModel MapToResultItem(Document doc)
        {
            return new ArticleSearchResultModel
            {
                Title = doc.Get(nameof(ArticleSearchResultModel.Title)) ?? string.Empty,
                Summary = doc.Get(ArticleSearchIndexingStrategy.SUMMARY_FIELD) ?? string.Empty,
                Url = doc.Get(nameof(ArticleSearchResultModel.Url)) ?? string.Empty,
                PublishedDate = DateTime.TryParse(
                    doc.Get(ArticleSearchIndexingStrategy.PUBLISHED_DATE_FIELD),
                    out var date
                ) ? date : DateTime.MinValue,
                ContentType = "Article"
            };
        }
    }
}
```

Register your search service in `Program.cs`:

```csharp
builder.Services.AddTransient<ArticleSearchService>();
```

## Step 6: Build the Search Interface

Finally, create a controller and views to let users search your content.

### 6.1: Create the Search Controller

```csharp
// Controllers/SearchController.cs
using Microsoft.AspNetCore.Mvc;
using YourProject.Search;

namespace YourProject.Controllers
{
    [Route("[controller]")]
    public class SearchController : Controller
    {
        private const string INDEX_NAME = "Article Search"; // Match the name from admin UI
        private readonly ArticleSearchService searchService;

        public SearchController(ArticleSearchService searchService)
        {
            this.searchService = searchService;
        }

        [HttpGet]
        public IActionResult Index(string? query, int pageSize = 10, int page = 1)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                // Show empty search page
                return View();
            }

            try
            {
                var results = searchService.Search(INDEX_NAME, query, pageSize, page);
                return View(results);
            }
            catch (Exception ex)
            {
                // Log the exception
                // return error view
                return View("Error");
            }
        }
    }
}
```

### 6.2: Create the Search View

Create a Razor view at `Views/Search/Index.cshtml`:

```cshtml
@model LuceneSearchResultModel<ArticleSearchResultModel>?
@{
    ViewData["Title"] = "Search";
}

<div class="search-page">
    <h1>Search Articles</h1>

    <form asp-action="Index" method="get" class="search-form">
        <div class="search-input-group">
            <input 
                type="text" 
                name="query" 
                value="@Model?.Query" 
                placeholder="Enter search terms..."
                class="search-input"
                autofocus
            />
            <button type="submit" class="search-button">Search</button>
        </div>
    </form>

    @if (Model != null)
    {
        <div class="search-results">
            @if (Model.TotalHits > 0)
            {
                <p class="search-stats">
                    Found @Model.TotalHits result@(Model.TotalHits == 1 ? "" : "s") for "@Model.Query"
                </p>

                @foreach (var item in Model.Hits)
                {
                    <article class="search-result-item">
                        <h2>
                            <a href="@item.Url">@item.Title</a>
                        </h2>
                        <p class="search-result-summary">@item.Summary</p>
                        <p class="search-result-meta">
                            <span class="content-type">@item.ContentType</span>
                            @if (item.PublishedDate != DateTime.MinValue)
                            {
                                <span class="published-date">
                                    Published: @item.PublishedDate.ToString("MMMM dd, yyyy")
                                </span>
                            }
                        </p>
                    </article>
                }

                @if (Model.TotalPages > 1)
                {
                    <nav class="search-pagination">
                        <ul>
                            @if (Model.Page > 1)
                            {
                                <li>
                                    <a asp-action="Index" 
                                       asp-route-query="@Model.Query" 
                                       asp-route-page="@(Model.Page - 1)"
                                       asp-route-pageSize="@Model.PageSize">
                                        Previous
                                    </a>
                                </li>
                            }

                            @for (int i = 1; i <= Model.TotalPages; i++)
                            {
                                <li class="@(i == Model.Page ? "active" : "")">
                                    @if (i == Model.Page)
                                    {
                                        <span>@i</span>
                                    }
                                    else
                                    {
                                        <a asp-action="Index" 
                                           asp-route-query="@Model.Query" 
                                           asp-route-page="@i"
                                           asp-route-pageSize="@Model.PageSize">
                                            @i
                                        </a>
                                    }
                                </li>
                            }

                            @if (Model.Page < Model.TotalPages)
                            {
                                <li>
                                    <a asp-action="Index" 
                                       asp-route-query="@Model.Query" 
                                       asp-route-page="@(Model.Page + 1)"
                                       asp-route-pageSize="@Model.PageSize">
                                        Next
                                    </a>
                                </li>
                            }
                        </ul>
                    </nav>
                }
            }
            else
            {
                <p class="no-results">
                    No results found for "@Model.Query". Try different search terms.
                </p>
            }
        </div>
    }
</div>

<style>
    .search-page {
        max-width: 800px;
        margin: 2rem auto;
        padding: 0 1rem;
    }

    .search-form {
        margin: 2rem 0;
    }

    .search-input-group {
        display: flex;
        gap: 0.5rem;
    }

    .search-input {
        flex: 1;
        padding: 0.75rem;
        font-size: 1rem;
        border: 1px solid #ddd;
        border-radius: 4px;
    }

    .search-button {
        padding: 0.75rem 2rem;
        background: #007bff;
        color: white;
        border: none;
        border-radius: 4px;
        cursor: pointer;
        font-size: 1rem;
    }

    .search-button:hover {
        background: #0056b3;
    }

    .search-stats {
        color: #666;
        margin-bottom: 1.5rem;
    }

    .search-result-item {
        margin-bottom: 2rem;
        padding-bottom: 2rem;
        border-bottom: 1px solid #eee;
    }

    .search-result-item h2 {
        margin: 0 0 0.5rem 0;
        font-size: 1.5rem;
    }

    .search-result-item h2 a {
        color: #1a0dab;
        text-decoration: none;
    }

    .search-result-item h2 a:hover {
        text-decoration: underline;
    }

    .search-result-summary {
        color: #545454;
        margin: 0.5rem 0;
    }

    .search-result-meta {
        color: #666;
        font-size: 0.875rem;
    }

    .search-result-meta .content-type {
        background: #e8f4f8;
        padding: 0.25rem 0.5rem;
        border-radius: 3px;
        margin-right: 0.5rem;
    }

    .search-pagination ul {
        display: flex;
        list-style: none;
        padding: 0;
        gap: 0.5rem;
        justify-content: center;
    }

    .search-pagination li.active span {
        font-weight: bold;
        color: #000;
    }

    .search-pagination a {
        padding: 0.5rem 0.75rem;
        border: 1px solid #ddd;
        border-radius: 4px;
        text-decoration: none;
        color: #007bff;
    }

    .search-pagination a:hover {
        background: #f8f9fa;
    }

    .no-results {
        padding: 2rem;
        text-align: center;
        color: #666;
        background: #f8f9fa;
        border-radius: 4px;
    }
</style>
```

## Step 7: Test Your Search

Now you're ready to test:

1. **Build and run your application**
2. **Create or modify some article pages** in Xperience (these will be automatically indexed)
3. **Navigate to `/Search`** in your browser
4. **Enter a search query** and verify that results appear
5. **Test pagination** by searching for common terms that return many results

## Optional: Adding Web Crawler for Full HTML Indexing

If you want to index the fully rendered HTML of your pages (including content from components and layouts), you can add web crawler functionality:

See the [Scraping web page content](Scraping-web-page-content.md) guide for detailed instructions.

Basic steps:

1. Add the `WebCrawlerBaseUrl` setting to your `appsettings.json`
2. Modify your indexing strategy to use `IWebCrawlerService`
3. Call the crawler to fetch and index rendered HTML content

## Troubleshooting

### Index is empty after rebuild

- Verify that your path configuration matches the actual Tree Path of your pages
- Check that the content type name in your strategy matches your actual content type
- Ensure the selected language matches the language of your content

### Search returns no results

- Verify that content was successfully indexed (check the rebuild completed successfully)
- Try a simpler query (single word) to test basic functionality
- Check that your query parsing logic handles special characters

### Changes to content don't appear in search

- Verify that the modified content matches your index configuration (path, content type, language)
- Check that your indexing strategy's `MapToLuceneDocumentOrNull` method isn't returning null
- Rebuild the index to ensure all content is processed

### Performance issues

- Consider indexing only essential fields
- Use `Field.Store.NO` for fields you don't need to retrieve
- Implement pagination with reasonable page sizes (10-50 results)

## Next Steps

Now that you have basic search working, you can:

- **Add more content types** to your indexing strategy
- **Implement faceted search** to allow filtering by categories - see [Custom index strategy](Custom-index-strategy.md)
- **Add search suggestions** or autocomplete functionality
- **Implement result highlighting** to show search terms in context
- **Add synonym support** or custom analyzers for better search quality
- **Configure auto-reindexing** for SaaS environments - see [Auto-Reindexing](Auto-Reindexing-After-Deployment.md)
- **Set up auto-scaling support** for multi-instance deployments - see [Auto-Scaling](Auto-Scaling.md)

For more advanced topics, see:

- [Custom index strategy](Custom-index-strategy.md) - Deep dive into indexing strategies
- [Search index querying](Search-index-querying.md) - Advanced query techniques
- [Text analyzing](Text-analyzing.md) - Using different analyzers
- [Indexing Secured Items](Indexing-Secured-Items.md) - Handling secured content

## Additional Resources

- [Terminology Guide](Terminology.md) - Understand key concepts and terms
- [DancingGoat Example](../examples/DancingGoat) - Working example implementation
- [Lucene.NET Documentation](https://lucenenet.apache.org/) - Official Lucene.NET docs
