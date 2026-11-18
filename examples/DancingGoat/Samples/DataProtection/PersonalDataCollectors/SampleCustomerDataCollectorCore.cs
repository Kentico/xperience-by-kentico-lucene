using System.Collections.Generic;
using System.Linq;

using CMS.Commerce;
using CMS.DataEngine;
using CMS.Globalization;

namespace Samples.DancingGoat
{
    /// <summary>
    /// Class responsible for retrieving customer's personal data. 
    /// </summary>
    internal class SampleCustomerDataCollectorCore
    {
        // Lists store Tuples of database column names and their corresponding display names.
        private readonly List<CollectedColumn> customerInfoColumns = new List<CollectedColumn> {
            new CollectedColumn(nameof(CustomerInfo.CustomerEmail), "Email"),
            new CollectedColumn(nameof(CustomerInfo.CustomerFirstName), "First name"),
            new CollectedColumn(nameof(CustomerInfo.CustomerLastName), "Last name"),
            new CollectedColumn(nameof(CustomerInfo.CustomerPhone), "Phone"),
            new CollectedColumn(nameof(CustomerInfo.CustomerCreatedWhen), "Created"),
            new CollectedColumn(nameof(CustomerInfo.CustomerGUID), "GUID")
        };

        private readonly List<CollectedColumn> customerAddressInfoColumns = new List<CollectedColumn> {
            new CollectedColumn(nameof(CustomerAddressInfo.CustomerAddressFirstName), "First name"),
            new CollectedColumn(nameof(CustomerAddressInfo.CustomerAddressLastName), "Last name"),
            new CollectedColumn(nameof(CustomerAddressInfo.CustomerAddressCompany), "Company"),
            new CollectedColumn(nameof(CustomerAddressInfo.CustomerAddressEmail), "Email"),
            new CollectedColumn(nameof(CustomerAddressInfo.CustomerAddressPhone), "Phone"),
            new CollectedColumn(nameof(CustomerAddressInfo.CustomerAddressLine1), "Address line 1"),
            new CollectedColumn(nameof(CustomerAddressInfo.CustomerAddressLine2), "Address line 2"),
            new CollectedColumn(nameof(CustomerAddressInfo.CustomerAddressCity), "City"),
            new CollectedColumn(nameof(CustomerAddressInfo.CustomerAddressZip), "ZIP"),
            new CollectedColumn(nameof(CustomerAddressInfo.CustomerAddressCountryID), ""),
            new CollectedColumn(nameof(CustomerAddressInfo.CustomerAddressStateID), ""),
            new CollectedColumn(nameof(CustomerAddressInfo.CustomerAddressGUID), "GUID")
        };

        private readonly List<CollectedColumn> orderAddressInfoColumns = new List<CollectedColumn> {
            new CollectedColumn(nameof(OrderAddressInfo.OrderAddressType), "Address type"),
            new CollectedColumn(nameof(OrderAddressInfo.OrderAddressFirstName), "First name"),
            new CollectedColumn(nameof(OrderAddressInfo.OrderAddressLastName), "Last name"),
            new CollectedColumn(nameof(OrderAddressInfo.OrderAddressCompany), "Company"),
            new CollectedColumn(nameof(OrderAddressInfo.OrderAddressEmail), "Email"),
            new CollectedColumn(nameof(OrderAddressInfo.OrderAddressPhone), "Phone"),
            new CollectedColumn(nameof(OrderAddressInfo.OrderAddressLine1), "Address line 1"),
            new CollectedColumn(nameof(OrderAddressInfo.OrderAddressLine2), "Address line 2"),
            new CollectedColumn(nameof(OrderAddressInfo.OrderAddressCity), "City"),
            new CollectedColumn(nameof(OrderAddressInfo.OrderAddressZip), "ZIP"),
            new CollectedColumn(nameof(OrderAddressInfo.OrderAddressCountryID), ""),
            new CollectedColumn(nameof(OrderAddressInfo.OrderAddressStateID), ""),
            new CollectedColumn(nameof(OrderAddressInfo.OrderAddressGUID), "GUID")
        };

        private readonly List<CollectedColumn> countryInfoColumns = new List<CollectedColumn> {
            new CollectedColumn(nameof(CountryInfo.CountryDisplayName), "Country")
        };

        private readonly List<CollectedColumn> stateInfoColumns = new List<CollectedColumn> {
            new CollectedColumn(nameof(StateInfo.StateDisplayName), "State")
        };

        private readonly List<CollectedColumn> orderInfoColumns = new List<CollectedColumn> {
            new CollectedColumn(nameof(OrderInfo.OrderNumber), "Order number"),
            new CollectedColumn(nameof(OrderInfo.OrderCreatedWhen), "Created"),
            new CollectedColumn(nameof(OrderInfo.OrderModifiedWhen), "Last modified"),
            new CollectedColumn(nameof(OrderInfo.OrderTotalPrice), "Total price"),
            new CollectedColumn(nameof(OrderInfo.OrderTotalShipping), "Shipping"),
            new CollectedColumn(nameof(OrderInfo.OrderTotalTax), "Tax"),
            new CollectedColumn(nameof(OrderInfo.OrderGrandTotal), "Grand total"),
            new CollectedColumn(nameof(OrderInfo.OrderPaymentMethodDisplayName), "Payment method"),
            new CollectedColumn(nameof(OrderInfo.OrderShippingMethodDisplayName), "Shipping method"),
            new CollectedColumn(nameof(OrderInfo.OrderShippingMethodPrice), "Shipping price"),
            new CollectedColumn(nameof(OrderInfo.OrderGUID), "GUID")
        };

        private readonly List<CollectedColumn> orderItemInfoColumns = new List<CollectedColumn> {
            new CollectedColumn(nameof(OrderItemInfo.OrderItemSKU), "SKU"),
            new CollectedColumn(nameof(OrderItemInfo.OrderItemName), "Name"),
            new CollectedColumn(nameof(OrderItemInfo.OrderItemQuantity), "Quantity"),
            new CollectedColumn(nameof(OrderItemInfo.OrderItemUnitPrice), "Unit price"),
            new CollectedColumn(nameof(OrderItemInfo.OrderItemTotalTax), "Tax"),
            new CollectedColumn(nameof(OrderItemInfo.OrderItemTaxRate), "Tax rate"),
            new CollectedColumn(nameof(OrderItemInfo.OrderItemTotalPrice), "Total price"),
            new CollectedColumn(nameof(OrderItemInfo.OrderItemGUID), "GUID")
        };


        private readonly IInfoProvider<CustomerAddressInfo> customerAddressInfoProvider;
        private readonly IInfoProvider<CountryInfo> countryInfoProvider;
        private readonly IInfoProvider<StateInfo> stateInfoProvider;
        private readonly IInfoProvider<OrderInfo> orderInfoProvider;
        private readonly IInfoProvider<OrderItemInfo> orderItemInfoProvider;
        private readonly IInfoProvider<OrderAddressInfo> orderAddressInfoProvider;


        /// <summary>
        /// Initializes a new instance of the <see cref="SampleCustomerDataCollectorCore"/>.
        /// </summary>
        /// <param name="customerAddressInfoProvider">Customer address info provider.</param>
        /// <param name="countryInfoProvider">Country info provider.</param>
        /// <param name="stateInfoProvider">State info provider.</param>
        /// <param name="orderInfoProvider">Order info provider.</param>
        /// <param name="orderItemInfoProvider">Order item info provider.</param>
        /// <param name="orderAddressInfoProvider">Order address info provider.</param>
        public SampleCustomerDataCollectorCore(
            IInfoProvider<CustomerAddressInfo> customerAddressInfoProvider,
            IInfoProvider<CountryInfo> countryInfoProvider,
            IInfoProvider<StateInfo> stateInfoProvider,
            IInfoProvider<OrderInfo> orderInfoProvider,
            IInfoProvider<OrderItemInfo> orderItemInfoProvider,
            IInfoProvider<OrderAddressInfo> orderAddressInfoProvider)
        {
            this.customerAddressInfoProvider = customerAddressInfoProvider;
            this.countryInfoProvider = countryInfoProvider;
            this.stateInfoProvider = stateInfoProvider;
            this.orderInfoProvider = orderInfoProvider;
            this.orderItemInfoProvider = orderItemInfoProvider;
            this.orderAddressInfoProvider = orderAddressInfoProvider;
        }


        /// <summary>
        /// Collect and format all customer personal data about given <paramref name="identities"/>.
        /// </summary>
        /// <param name="identities">Identities to collect data about.</param>
        /// <param name="writer">Writer to format output data.</param>
        public void CollectData(IEnumerable<BaseInfo> identities, IPersonalDataWriter writer)
        {
            var customers = identities.OfType<CustomerInfo>().ToList();
            if (!customers.Any())
            {
                return;
            }

            var customerIds = customers.Select(c => c.CustomerID).ToList();

            var addresses = customerAddressInfoProvider.Get()
                                                       .WhereIn(nameof(CustomerAddressInfo.CustomerAddressCustomerID), customerIds)
                                                       .ToList();

            var orders = orderInfoProvider.Get()
                                          .WhereIn(nameof(OrderInfo.OrderCustomerID), customerIds)
                                          .ToList();

            var orderIds = orders.Select(o => o.OrderID).ToList();
            var orderItems = orderItemInfoProvider.Get()
                                                  .WhereIn(nameof(OrderItemInfo.OrderItemOrderID), orderIds)
                                                  .ToList();

            var orderAddresses = orderAddressInfoProvider.Get()
                                                         .WhereIn(nameof(OrderAddressInfo.OrderAddressOrderID), orderIds)
                                                         .ToList();

            var addressesByCustomerId = addresses.GroupBy(a => a.CustomerAddressCustomerID)
                .ToDictionary(g => g.Key, g => g.ToList());

            var ordersByCustomerId = orders.GroupBy(o => o.OrderCustomerID)
                .ToDictionary(g => g.Key, g => g.ToList());

            var itemsByOrderId = orderItems.GroupBy(i => i.OrderItemOrderID)
                .ToDictionary(g => g.Key, g => g.ToList());

            var orderAddressesByOrderId = orderAddresses.GroupBy(a => a.OrderAddressOrderID)
                .ToDictionary(g => g.Key, g => g.ToList());

            writer.WriteStartSection("CustomerData", "Customer data");

            foreach (var customerInfo in customers)
            {
                addressesByCustomerId.TryGetValue(customerInfo.CustomerID, out var customerAddresses);
                ordersByCustomerId.TryGetValue(customerInfo.CustomerID, out var customerOrders);

                WriteCustomerInfo(customerInfo, customerAddresses, customerOrders, itemsByOrderId, orderAddressesByOrderId, writer);
            }

            writer.WriteEndSection();
        }


        /// <summary>
        /// Writes base info for given customer to the current writer.
        /// </summary>
        private void WriteCustomerInfo(CustomerInfo customerInfo, IEnumerable<CustomerAddressInfo> customerAddresses, IEnumerable<OrderInfo> customerOrders, Dictionary<int, List<OrderItemInfo>> itemsByOrderId, Dictionary<int, List<OrderAddressInfo>> orderAddressesByOrderId, IPersonalDataWriter writer)
        {
            writer.WriteStartSection(CustomerInfo.OBJECT_TYPE, "Customer");
            writer.WriteBaseInfo(customerInfo, customerInfoColumns);

            WriteCustomerAddresses(customerAddresses, writer);
            WriteCustomerOrders(customerOrders, itemsByOrderId, orderAddressesByOrderId, writer);

            writer.WriteEndSection();
        }


        /// <summary>
        /// Writes all addresses for a given customer.
        /// </summary>
        private void WriteCustomerAddresses(IEnumerable<CustomerAddressInfo> customerAddresses, IPersonalDataWriter writer)
        {
            foreach (var address in customerAddresses)
            {
                writer.WriteStartSection(CustomerAddressInfo.OBJECT_TYPE, "Customer address");
                writer.WriteBaseInfo(address, customerAddressInfoColumns);

                var countryID = address.CustomerAddressCountryID;
                var stateID = address.CustomerAddressStateID;
                if (countryID != 0)
                {
                    writer.WriteBaseInfo(countryInfoProvider.Get(countryID), countryInfoColumns);
                }
                if (stateID != 0)
                {
                    writer.WriteBaseInfo(stateInfoProvider.Get(stateID), stateInfoColumns);
                }

                writer.WriteEndSection();
            }
        }


        /// <summary>
        /// Writes all orders for a given customer.
        /// </summary>
        private void WriteCustomerOrders(IEnumerable<OrderInfo> customerOrders, Dictionary<int, List<OrderItemInfo>> itemsByOrderId, Dictionary<int, List<OrderAddressInfo>> orderAddressesByOrderId, IPersonalDataWriter writer)
        {
            foreach (var order in customerOrders)
            {
                writer.WriteStartSection(OrderInfo.OBJECT_TYPE, "Order");
                writer.WriteBaseInfo(order, orderInfoColumns);

                if (orderAddressesByOrderId != null && orderAddressesByOrderId.TryGetValue(order.OrderID, out var addresses))
                {
                    WriteOrderAddresses(addresses, writer);
                }

                if (itemsByOrderId != null && itemsByOrderId.TryGetValue(order.OrderID, out var items))
                {
                    WriteOrderItems(items, writer);
                }

                writer.WriteEndSection();
            }
        }

        /// <summary>
        /// Writes all order addresses for a given order.
        /// </summary>
        private void WriteOrderAddresses(IEnumerable<OrderAddressInfo> addresses, IPersonalDataWriter writer)
        {
            foreach (var address in addresses)
            {
                writer.WriteStartSection(OrderAddressInfo.OBJECT_TYPE, "Order address");
                writer.WriteBaseInfo(address, orderAddressInfoColumns);

                var countryID = address.OrderAddressCountryID;
                var stateID = address.OrderAddressStateID;
                if (countryID != 0)
                {
                    writer.WriteBaseInfo(countryInfoProvider.Get(countryID), countryInfoColumns);
                }
                if (stateID != 0)
                {
                    writer.WriteBaseInfo(stateInfoProvider.Get(stateID), stateInfoColumns);
                }

                writer.WriteEndSection();
            }
        }

        /// <summary>
        /// Writes all items for a given order.
        /// </summary>
        private void WriteOrderItems(IEnumerable<OrderItemInfo> items, IPersonalDataWriter writer)
        {
            foreach (var item in items)
            {
                writer.WriteStartSection(OrderItemInfo.OBJECT_TYPE, "Order item");
                writer.WriteBaseInfo(item, orderItemInfoColumns);
                writer.WriteEndSection();
            }
        }
    }
}
