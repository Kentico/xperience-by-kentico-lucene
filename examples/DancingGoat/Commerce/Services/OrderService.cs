using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CMS.Commerce;
using CMS.DataEngine;

using DancingGoat.Models;

namespace DancingGoat.Commerce;

/// <summary>
/// Service for managing orders.
/// </summary>
public sealed class OrderService
{
    private readonly IInfoProvider<ShippingMethodInfo> shippingMethodInfoProvider;
    private readonly IOrderCreationService<OrderData, DancingGoatPriceCalculationRequest, DancingGoatPriceCalculationResult, AddressDto> orderCreationService;
    private readonly OrderNumberGenerator orderNumberGenerator;

    public OrderService(
        IInfoProvider<ShippingMethodInfo> shippingMethodInfoProvider,
        IOrderCreationService<OrderData, DancingGoatPriceCalculationRequest, DancingGoatPriceCalculationResult, AddressDto> orderCreationService,
        OrderNumberGenerator orderNumberGenerator)
    {
        this.shippingMethodInfoProvider = shippingMethodInfoProvider;
        this.orderCreationService = orderCreationService;
        this.orderNumberGenerator = orderNumberGenerator;
    }


    /// <summary>
    /// Creates an order based on the provided shopping cart and customer information.
    /// </summary>
    /// <returns>Returns order number of newly create order.</returns>
    public async Task<int> CreateOrder(ShoppingCartDataModel shoppingCartData, CustomerViewModel customer, CustomerAddressViewModel billingAddress, ShippingAddressViewModel shippingAddress,
        int memberId, string languageName, int paymentMethodId, int shippingMethodId, decimal expectedShippingPrice, CancellationToken cancellationToken)
    {
        var shipping = (await shippingMethodInfoProvider.GetAsync(shippingMethodId, cancellationToken));

        if (shipping == null)
        {
            throw new InvalidOperationException("Invalid shipping method.");
        }
        if (expectedShippingPrice < shipping.ShippingMethodPrice)
        {
            throw new InvalidOperationException("Different shipping price than expected by the customer.");
        }

        var orderData = new OrderData()
        {
            OrderItems = shoppingCartData.Items.Select(item => new OrderItem()
            {
                ProductIdentifier = item.ProductIdentifier,
                Quantity = item.Quantity
            }),
            BillingAddress = ConvertAddress(billingAddress, customer),
            ShippingAddress = !shippingAddress.IsSameAsBilling ? ConvertAddress(shippingAddress, customer) : null,
            MemberId = memberId,
            PaymentMethodId = paymentMethodId,
            ShippingMethodId = shippingMethodId,
            OrderNumber = await orderNumberGenerator.GenerateOrderNumber(cancellationToken),
            LanguageName = languageName
        };

        var orderId = await orderCreationService.CreateOrder(orderData, cancellationToken);

        return orderId;
    }


    private static AddressDto ConvertAddress(CustomerAddressViewModel customerAddress, CustomerViewModel customer)
    {
        int.TryParse(customerAddress.CountryId, out var countryId);
        int.TryParse(customerAddress.StateId, out var stateId);

        return new AddressDto()
        {
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            Company = customer.Company,
            Email = customer.Email,
            Phone = customer.PhoneNumber,
            Line1 = customerAddress.Line1,
            Line2 = customerAddress.Line2,
            City = customerAddress.City,
            Zip = customerAddress.PostalCode,
            CountryID = countryId,
            StateID = stateId,
        };
    }
}
