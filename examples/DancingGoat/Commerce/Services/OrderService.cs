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
    private readonly CalculationService calculationService;

    public OrderService(
        IInfoProvider<ShippingMethodInfo> shippingMethodInfoProvider,
        IOrderCreationService<OrderData, DancingGoatPriceCalculationRequest, DancingGoatPriceCalculationResult, AddressDto> orderCreationService,
        OrderNumberGenerator orderNumberGenerator,
        CalculationService calculationService)
    {
        this.shippingMethodInfoProvider = shippingMethodInfoProvider;
        this.orderCreationService = orderCreationService;
        this.orderNumberGenerator = orderNumberGenerator;
        this.calculationService = calculationService;
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

        // A shipping promotion can reduce the shipping method's price to zero. Comparing the expected price
        // against the raw shipping method price would reject checkout for a cart that qualifies for the promotion.
        var calculationResult = await calculationService.Calculate(shoppingCartData, PriceCalculationMode.Checkout, shippingMethodId, paymentMethodId, billingAddress, cancellationToken);
        if (expectedShippingPrice < calculationResult.ShippingPrice)
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
            BuyerIdentifier = BuyerIdentifier.FromMemberId(memberId),
            PaymentMethodId = paymentMethodId,
            ShippingMethodId = shippingMethodId,
            OrderNumber = await orderNumberGenerator.GenerateOrderNumber(cancellationToken),
            CouponCodes = shoppingCartData.CouponCodes,
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
