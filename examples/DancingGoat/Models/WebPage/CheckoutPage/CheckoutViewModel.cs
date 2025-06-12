namespace DancingGoat.Models;

public sealed record CheckoutViewModel(CheckoutStep Step, CustomerViewModel Customer, CustomerAddressViewModel CustomerAddress, ShoppingCartViewModel ShoppingCart);
