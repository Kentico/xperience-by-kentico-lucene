using System.Collections.Generic;
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
public sealed class PaymentRepository : CachedRepositoryBase
{
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
        : base(websiteChannelContext, cache, cacheDependencyBuilderFactory)
    {
        this.paymentMethodInfoProvider = paymentMethodInfoProvider;
    }


    /// <summary>
    /// Returns a cached list of all <see cref="PaymentMethodInfo"/>.
    /// </summary>
    public async Task<IEnumerable<PaymentMethodInfo>> GetPayments(CancellationToken cancellationToken)
    {
        return await GetCached(GetPaymentInternal, [nameof(PaymentRepository), nameof(GetPayments)], cancellationToken);
    }


    private async Task<IEnumerable<PaymentMethodInfo>> GetPaymentInternal(CancellationToken cancellationToken)
    {
        return await paymentMethodInfoProvider.Get()
                                              .WhereTrue(nameof(PaymentMethodInfo.PaymentMethodEnabled))
                                              .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);
    }
}
