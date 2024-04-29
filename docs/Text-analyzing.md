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

Now the `CzechAnalyzer` will be available for selection in the Admin UI under Lucene Analyzer. This analyzer will be used by selected strategy for reindexing. You can retrieve and use this analyzer for index quierying. The analyzer will be available on the instance of the `LuceneIndex` class under `LuceneAnalyzer`.

    ```csharp
    public class SimpleSearchService
    {
        // Other class members ...
        public LuceneSearchResultModel<DancingGoatSearchResultModel> GlobalSearch(
        string indexName,
        string? searchText,
        int pageSize = 20,
        int page = 1)
        {
            var index = luceneIndexManager.GetRequiredIndex(indexName);
            var query = GetTermQuery(searchText, index);
            // ...
        }

        private Query GetTermQuery(string? searchText, LuceneIndex index)
        {
            // Here we retrieve the analyzer instance which we can use for our queries
            var analyzer = index.LuceneAnalyzer;
            var queryBuilder = new QueryBuilder(analyzer);

            var booleanQuery = new BooleanQuery();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                booleanQuery = AddToTermQuery(booleanQuery, queryBuilder.CreatePhraseQuery(nameof(DancingGoatSearchResultModel.Title), searchText, PHRASE_SLOP), 5);
                booleanQuery = AddToTermQuery(booleanQuery, queryBuilder.CreateBooleanQuery(nameof(DancingGoatSearchResultModel.Title), searchText, Occur.SHOULD), 0.5f);

                if (booleanQuery.GetClauses().Count() > 0)
                {
                    return booleanQuery;
                }
            }

            return new MatchAllDocsQuery();
        }

        // Other class members ...
    }
    ```

The `Analyzer` uses a `LuceneVersion` to match version compatibility accross releases of Lucene. By default we use the latest `LuceneVersion.LUCENE_48`. You can override the version when registering application services as follows.

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