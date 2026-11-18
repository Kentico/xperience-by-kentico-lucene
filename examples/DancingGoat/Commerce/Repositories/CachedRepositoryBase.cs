using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CMS.DataEngine;
using CMS.Helpers;
using CMS.Websites.Routing;

namespace DancingGoat.Commerce;

/// <summary>
/// Base class for repositories returning cached lists of info objects.
/// </summary>
public abstract class CachedRepositoryBase
{
    private const int CACHE_EXPIRATION_MINUTES = 5;

    private readonly IWebsiteChannelContext websiteChannelContext;
    private readonly IProgressiveCache cache;
    private readonly ICacheDependencyBuilderFactory cacheDependencyBuilderFactory;


    /// <summary>
    /// Initializes a new instance of the <see cref="CachedRepositoryBase"/> class.
    /// </summary>
    /// <param name="websiteChannelContext">The website channel context.</param>
    /// <param name="cache">The cache.</param>
    /// <param name="cacheDependencyBuilderFactory">The cache dependency builder factory.</param>
    protected CachedRepositoryBase(
        IWebsiteChannelContext websiteChannelContext,
        IProgressiveCache cache,
        ICacheDependencyBuilderFactory cacheDependencyBuilderFactory)
    {
        this.websiteChannelContext = websiteChannelContext;
        this.cache = cache;
        this.cacheDependencyBuilderFactory = cacheDependencyBuilderFactory;
    }


    /// <summary>
    /// Returns a cached result of the given <paramref name="loader"/>.
    /// Preview requests bypass the cache to always work with fresh data.
    /// The cache entry is invalidated when any info object of type <typeparamref name="TInfo"/> changes.
    /// </summary>
    /// <typeparam name="TInfo">Type of the retrieved info objects.</typeparam>
    /// <param name="loader">Function loading the info objects when the cache entry is missing.</param>
    /// <param name="cacheKeyParts">Parts identifying the cache entry, typically the repository name, method name and parameter values.</param>
    /// <param name="cancellationToken">Cancellation instruction.</param>
    protected async Task<IEnumerable<TInfo>> GetCached<TInfo>(
        Func<CancellationToken, Task<IEnumerable<TInfo>>> loader,
        object[] cacheKeyParts,
        CancellationToken cancellationToken)
        where TInfo : BaseInfo
    {
        if (websiteChannelContext.IsPreview)
        {
            return await loader(cancellationToken);
        }

        var cacheSettings = new CacheSettings(CACHE_EXPIRATION_MINUTES, [websiteChannelContext.WebsiteChannelName, .. cacheKeyParts]);

        return await cache.LoadAsync(async (cacheSettings, token) =>
        {
            var result = await loader(token);

            if (cacheSettings.Cached = result != null && result.Any())
            {
                cacheSettings.CacheDependency = cacheDependencyBuilderFactory.Create()
                    .ForInfoObjects<TInfo>()
                    .All()
                    .Builder()
                    .Build();
            }

            return result;
        }, cacheSettings, cancellationToken);
    }
}
