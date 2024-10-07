using CMS.Helpers;

namespace Kentico.Xperience.Lucene.Core.Indexing;

/// <summary>
///  Manages adding and getting of Lucene indexes.
///  Uses progressive caching for faster index operations.
/// </summary>
internal class DefaultLuceneIndexManager : ILuceneIndexManager
{
    private readonly ILuceneConfigurationStorageService storageService;
    private readonly IProgressiveCache progressiveCache;

    public DefaultLuceneIndexManager(ILuceneConfigurationStorageService storageService, IProgressiveCache progressiveCache)
    {
        this.storageService = storageService;
        this.progressiveCache = progressiveCache;
    }

    /// <inheritdoc />
    public IEnumerable<LuceneIndex> GetAllIndices()
    {
        var indices = (CacheSettings cs) =>
        {
            var luceneIndices = storageService.GetAllIndexDataAsync().Result.Select(x => new LuceneIndex(x, StrategyStorage.Strategies, AnalyzerStorage.Analyzers, AnalyzerStorage.AnalyzerLuceneVersion));

            cs.CacheDependency = CacheHelper.GetCacheDependency(GetLuceneDependencyCacheKeys());

            return luceneIndices;
        };

        return progressiveCache.Load(cs => indices(cs), new CacheSettings(10, $"customdatasource|index|all"));
    }

    /// <inheritdoc />
    public LuceneIndex? GetIndex(string indexName)
    {
        var index = (CacheSettings cs) =>
        {
            var indexConfiguration = storageService.GetIndexDataOrNullAsync(indexName).Result;

            if (indexConfiguration == null)
            {
                return default;
            }
            cs.CacheDependency = CacheHelper.GetCacheDependency(GetLuceneDependencyCacheKeys());

            return new LuceneIndex(indexConfiguration, StrategyStorage.Strategies, AnalyzerStorage.Analyzers, AnalyzerStorage.AnalyzerLuceneVersion);
        };

        return progressiveCache.Load(cs => index(cs), new CacheSettings(10, $"customdatasource|index|name|{indexName}"));
    }

    /// <inheritdoc />
    public LuceneIndex? GetIndex(int identifier)
    {
        var index = (CacheSettings cs) =>
        {
            var indexConfiguration = storageService.GetIndexDataOrNullAsync(identifier).Result;

            if (indexConfiguration == null)
            {
                return default;
            }
            cs.CacheDependency = CacheHelper.GetCacheDependency(GetLuceneDependencyCacheKeys());

            return new LuceneIndex(indexConfiguration, StrategyStorage.Strategies, AnalyzerStorage.Analyzers, AnalyzerStorage.AnalyzerLuceneVersion);
        };

        return progressiveCache.Load(cs => index(cs), new CacheSettings(10, $"customdatasource|index|identifier|{identifier}"));
    }

    /// <inheritdoc />
    public LuceneIndex GetRequiredIndex(string indexName)
    {
        var index = (CacheSettings cs) =>
        {
            var indexConfiguration = storageService.GetIndexDataOrNullAsync(indexName).Result ?? throw new InvalidOperationException($"The index '{indexName}' does not exist.");
            cs.CacheDependency = CacheHelper.GetCacheDependency(GetLuceneDependencyCacheKeys());

            return new LuceneIndex(indexConfiguration, StrategyStorage.Strategies, AnalyzerStorage.Analyzers, AnalyzerStorage.AnalyzerLuceneVersion);
        };

        return progressiveCache.Load(cs => index(cs), new CacheSettings(10, $"customdatasource|index|identifier|{indexName}"));
    }

    /// <inheritdoc />
    public void AddIndex(LuceneIndexModel indexConfiguration)
    {
        if (indexConfiguration == null)
        {
            throw new ArgumentNullException(nameof(indexConfiguration));
        }

        if (GetAllIndices().Any(i => i.IndexName.Equals(indexConfiguration.IndexName, StringComparison.OrdinalIgnoreCase) || indexConfiguration.Id == i.Identifier))
        {
            throw new InvalidOperationException($"Attempted to register Lucene index with identifer [{indexConfiguration.Id}] and name [{indexConfiguration.IndexName}] but it is already registered.");
        }

        storageService.TryCreateIndex(indexConfiguration);
    }

    private ISet<string> GetLuceneDependencyCacheKeys()
    {
        var dependencyCacheKeys = new HashSet<string>
        {
            CacheHelper.BuildCacheItemName(new[] { LuceneIndexItemInfo.OBJECT_TYPE,"all" }, lowerCase: false),
            CacheHelper.BuildCacheItemName(new[] { LuceneIndexLanguageItemInfo.OBJECT_TYPE,"all" }, lowerCase: false),
            CacheHelper.BuildCacheItemName(new[] { LuceneIncludedPathItemInfo.OBJECT_TYPE,"all" }, lowerCase: false),
            CacheHelper.BuildCacheItemName(new[] { LuceneContentTypeItemInfo.OBJECT_TYPE,"all" }, lowerCase: false)
        };
        return dependencyCacheKeys;
    }
}

