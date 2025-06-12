namespace DancingGoat.Models;

/// <summary>
/// Represents the steps in the checkout process.
/// </summary>
public enum CheckoutStep
{
    /// <summary>
    /// Represents the step for entering customer shipping information.
    /// </summary>
    CheckoutCustomer = 1,

    /// <summary>
    /// Represents the step for confirming the order.
    /// </summary>
    OrderConfirmation = 2
}
