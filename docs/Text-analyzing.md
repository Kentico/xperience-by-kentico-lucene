# Select Lucene text Analyzer

In the admin UI you are able to select an `Analyzer` which will be used by selected strategy to rebuild an index. By default the only available `Analyzer` is the `StandardAnalyzer`. In order to use a different analyzer you will have to register it, along with the Lucene, to the application services.

```csharp
// Program.cs

// Registers all services and enables custom indexing behavior
services.AddKenticoLucene(builder =>
{
    // Register strategies ...
    builder.RegisterAnalyzer<CzechAnalyzer>("Czech analyzer");
});
```

Now the `CzechAnalyzer` will be available for selection in the Admin UI under Lucene Analyzer. This analyzer will be used by selected strategy for reindexing. You can retrieve and use this analyzer for index querying. The analyzer will be available on the instance of the `LuceneIndex` class under `LuceneAnalyzer`.

```csharp
public class SearchService
{
    private const int PHRASE_SLOP = 3;

    private readonly ILuceneIndexManager luceneIndexManager;

    // Other class members ...

    public LuceneSearchResultModel<GlobalSearchResultModel> GlobalSearch(
        string indexName,
        string? searchText,
        int pageSize = 20,
        int page = 1)
    {
        var index = luceneIndexManager.GetRequiredIndex(indexName);
        var query = GetTermQuery(searchText, index);
        // ...
    }

    private static Query GetTermQuery(string? searchText, LuceneIndex index)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return new MatchAllDocsQuery();
        }

        // Here we retrieve the analyzer instance which we can use for our queries
        var analyzer = index.LuceneAnalyzer;
        var queryBuilder = new QueryBuilder(analyzer);

        var booleanQuery = new BooleanQuery();

        AddQuery(booleanQuery, queryBuilder.CreatePhraseQuery(nameof(GlobalSearchResultModel.Title), searchText, PHRASE_SLOP), 5);
        AddQuery(booleanQuery, queryBuilder.CreateBooleanQuery(nameof(GlobalSearchResultModel.Title), searchText, Occur.SHOULD), 0.5f);

        return booleanQuery.GetClauses().Length > 0
            ? booleanQuery
            : new MatchAllDocsQuery();
    }

    private static void AddQuery(BooleanQuery booleanQuery, Query? query, float boost)
    {
        // QueryBuilder returns null when the analyzer produces no terms
        if (query is null)
        {
            return;
        }

        query.Boost = boost;
        booleanQuery.Add(query, Occur.SHOULD);
    }
}
```

See [`examples/DancingGoat/Search/DancingGoatSearchService.cs`](../examples/DancingGoat/Search/DancingGoatSearchService.cs) for a working implementation that uses `index.LuceneAnalyzer` for both query building and result highlighting.

The `Analyzer` uses a `LuceneVersion` to match version compatibility across releases of Lucene. By default we use the latest `LuceneVersion.LUCENE_48`. You can override the version when registering application services as follows.

```csharp
// Program.cs

// Registers all services and enables custom indexing behavior
services.AddKenticoLucene(builder =>
{
    // Register strategies ...
    // Register analyzers
    builder.SetAnalyzerLuceneVersion(LuceneVersion.LUCENE_47);
});
```

## Disable default standard analyzer

By default, the `StandardAnalyzer` is included and available for use. If you do not want the default `StandardAnalyzer` to appear in the Admin UI, you can disable it by setting the `IncludeDefaultAnalyzer` property to `false` when registering application services.

```csharp
// Program.cs

// Registers all services and disables the default StandardAnalyzer
services.AddKenticoLucene(builder =>
{
    builder.IncludeDefaultAnalyzer = false;

    // Register custom analyzers
    builder.RegisterAnalyzer<CzechAnalyzer>("Czech analyzer");
});
```

With this configuration, the `StandardAnalyzer` will no longer be available in the Admin UI, and only the analyzers you explicitly register will appear.
