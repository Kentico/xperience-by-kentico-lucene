using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CMS.Commerce;
using CMS.DataEngine;

#pragma warning disable KXE0002 // Commerce feature is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
namespace DancingGoat.Commerce;

/// <summary>
/// Service for customer data retrieval.
/// </summary>
public sealed class CustomerDataRetriever
{
    private readonly IInfoProvider<CustomerInfo> customerInfoProvider;
    private readonly IInfoProvider<CustomerAddressInfo> customerAddressInfoProvider;


    public CustomerDataRetriever(IInfoProvider<CustomerInfo> customerInfoProvider, IInfoProvider<CustomerAddressInfo> customerAddressInfoProvider)
    {
        this.customerInfoProvider = customerInfoProvider;
        this.customerAddressInfoProvider = customerAddressInfoProvider;
    }


    /// <summary>
    /// Returns a customer object for the given member ID.
    /// </summary>
    public async Task<CustomerInfo> GetCustomerForMember(int memberId, CancellationToken cancellationToken)
    {
        return (await customerInfoProvider
                .Get()
                .WhereEquals(nameof(CustomerInfo.CustomerMemberID), memberId)
                .TopN(1)
                .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken))
            .FirstOrDefault();
    }


    /// <summary>
    /// Returns a customer address object for the given customer ID.
    /// </summary>
    public async Task<CustomerAddressInfo> GetCustomerAddress(int customerId, CancellationToken cancellationToken)
    {
        return (await customerAddressInfoProvider
                .Get()
                .WhereEquals(nameof(CustomerAddressInfo.CustomerAddressCustomerID), customerId)
                .TopN(1)
                .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken))
            .FirstOrDefault();
    }
}
#pragma warning restore KXE0002 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
