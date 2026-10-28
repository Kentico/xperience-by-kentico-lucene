namespace DancingGoat.Models
{
    /// <summary>
    /// A cart item discount the product detail advertises.
    /// </summary>
    /// <param name="Message">
    /// Text telling the customer how to get the discount with the product. It names the coupon code to use and
    /// the need to sign in when the promotion requires them.
    /// </param>
    public record ProductCartItemDiscountViewModel(string Message);
}
