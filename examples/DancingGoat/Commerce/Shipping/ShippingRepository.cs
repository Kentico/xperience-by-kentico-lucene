using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CMS.Commerce;
using CMS.DataEngine;
using CMS.Helpers;
using CMS.Websites.Routing;

namespace DancingGoat.Commerce;

/// <summary>
/// Repository for managing shipping method information retrieval operations.
/// </summary>
public class ShippingRepository
{
    private readonly IWebsiteChannelContext websiteChannelContext;
    private readonly IProgressiveCache cache;
    private readonly ICacheDependencyBuilderFactory cacheDependencyBuilderFactory;
    private readonly IInfoProvider<ShippingMethodInfo> shippingMethodInfoProvider;


    /// <summary>
    /// Initializes a new instance of the <see cref="ShippingRepository"/> class.
    /// </summary>
    /// <param name="websiteChannelContext">The website channel context.</param>
    /// <param name="cache">The cache.</param>
    /// <param name="cacheDependencyBuilderFactory">The cache dependency builder factory.</param>
    /// <param name="shippingMethodInfoProvider">The shipping method info provider.</param>
    public ShippingRepository(IWebsiteChannelContext websiteChannelContext, IProgressiveCache cache, ICacheDependencyBuilderFactory cacheDependencyBuilderFactory,
                              IInfoProvider<ShippingMethodInfo> shippingMethodInfoProvider)
    {
        this.websiteChannelContext = websiteChannelContext;
        this.cache = cache;
        this.cacheDependencyBuilderFactory = cacheDependencyBuilderFactory;
        this.shippingMethodInfoProvider = shippingMethodInfoProvider;
    }


    /// <summary>
    /// Returns a cached list of all <see cref="ShippingMethodInfo"/>.
    /// </summary>
    public async Task<IEnumerable<ShippingMethodInfo>> GetShipping(CancellationToken cancellationToken)
    {
        if (websiteChannelContext.IsPreview)
        {
            return await GetShippingInternal(cancellationToken);
        }

        var cacheSettings = new CacheSettings(5, websiteChannelContext.WebsiteChannelName, nameof(ShippingRepository), nameof(GetShipping));

        return await cache.LoadAsync(async (cacheSettings) =>
        {
            var result = await GetShippingInternal(cancellationToken);

            if (cacheSettings.Cached = result != null && result.Any())
            {
                var cacheDependencyBuilder = cacheDependencyBuilderFactory.Create();
                var cacheDependencies = cacheDependencyBuilder
                                        .ForInfoObjects<ShippingMethodInfo>()
                                        .All()
                                        .Builder()
                                        .Build();
                cacheSettings.CacheDependency = cacheDependencies;
            }

            return result;
        }, cacheSettings);
    }


    private async Task<IEnumerable<ShippingMethodInfo>> GetShippingInternal(CancellationToken cancellationToken)
    {
        return await shippingMethodInfoProvider.Get()
                                               .WhereTrue(nameof(ShippingMethodInfo.ShippingMethodEnabled))
                                               .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);
    }
}
