using System.Collections.Generic;
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
public sealed class ShippingRepository : CachedRepositoryBase
{
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
        : base(websiteChannelContext, cache, cacheDependencyBuilderFactory)
    {
        this.shippingMethodInfoProvider = shippingMethodInfoProvider;
    }


    /// <summary>
    /// Returns a cached list of all <see cref="ShippingMethodInfo"/>.
    /// </summary>
    public async Task<IEnumerable<ShippingMethodInfo>> GetShipping(CancellationToken cancellationToken)
    {
        return await GetCached(GetShippingInternal, [nameof(ShippingRepository), nameof(GetShipping)], cancellationToken);
    }


    private async Task<IEnumerable<ShippingMethodInfo>> GetShippingInternal(CancellationToken cancellationToken)
    {
        return await shippingMethodInfoProvider.Get()
                                               .WhereTrue(nameof(ShippingMethodInfo.ShippingMethodEnabled))
                                               .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);
    }
}
