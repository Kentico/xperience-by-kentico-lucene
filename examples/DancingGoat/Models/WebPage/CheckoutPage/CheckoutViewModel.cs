﻿namespace DancingGoat.Models;

public sealed record CheckoutViewModel(CheckoutStep Step, CustomerViewModel Customer, CustomerAddressViewModel BillingAddress, ShippingAddressViewModel ShippingAddress, ShoppingCartViewModel ShoppingCart, PaymentShippingViewModel PaymentShipping);
