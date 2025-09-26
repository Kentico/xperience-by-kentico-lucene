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
/// Repository for managing payment method information retrieval operations.
/// </summary>
public class PaymentRepository
{
    private readonly IWebsiteChannelContext websiteChannelContext;
    private readonly IProgressiveCache cache;
    private readonly ICacheDependencyBuilderFactory cacheDependencyBuilderFactory;
    private readonly IInfoProvider<PaymentMethodInfo> paymentMethodInfoProvider;


    /// <summary>
    /// Initializes a new instance of the <see cref="PaymentRepository"/> class.
    /// </summary>
    /// <param name="websiteChannelContext">The website channel context.</param>
    /// <param name="cache">The cache.</param>
    /// <param name="cacheDependencyBuilderFactory">The cache dependency builder factory.</param>
    /// <param name="paymentMethodInfoProvider">The payment method info provider.</param>
    public PaymentRepository(IWebsiteChannelContext websiteChannelContext, IProgressiveCache cache, ICacheDependencyBuilderFactory cacheDependencyBuilderFactory,
                             IInfoProvider<PaymentMethodInfo> paymentMethodInfoProvider)
    {
        this.websiteChannelContext = websiteChannelContext;
        this.cache = cache;
        this.cacheDependencyBuilderFactory = cacheDependencyBuilderFactory;
        this.paymentMethodInfoProvider = paymentMethodInfoProvider;
    }


    /// <summary>
    /// Returns a cached list of all <see cref="PaymentMethodInfo"/>.
    /// </summary>
    public async Task<IEnumerable<PaymentMethodInfo>> GetPayments(CancellationToken cancellationToken)
    {
        if (websiteChannelContext.IsPreview)
        {
            return await GetPaymentInternal(cancellationToken);
        }

        var cacheSettings = new CacheSettings(5, websiteChannelContext.WebsiteChannelName, nameof(PaymentRepository), nameof(GetPayments));

        return await cache.LoadAsync(async (cacheSettings) =>
        {
            var result = await GetPaymentInternal(cancellationToken);

            if (cacheSettings.Cached = result != null && result.Any())
            {
                var cacheDependencyBuilder = cacheDependencyBuilderFactory.Create();
                var cacheDependencies = cacheDependencyBuilder
                                        .ForInfoObjects<PaymentMethodInfo>()
                                        .All()
                                        .Builder()
                                        .Build();
                cacheSettings.CacheDependency = cacheDependencies;
            }
            return result;
        }, cacheSettings);
    }


    private async Task<IEnumerable<PaymentMethodInfo>> GetPaymentInternal(CancellationToken cancellationToken)
    {
        return await paymentMethodInfoProvider.Get()
                                              .WhereTrue(nameof(PaymentMethodInfo.PaymentMethodEnabled))
                                              .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);
    }
}
