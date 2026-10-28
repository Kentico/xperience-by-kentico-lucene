namespace DancingGoat.Models
{
    /// <summary>
    /// A free shipping promotion the product detail advertises.
    /// </summary>
    /// <param name="Message">
    /// Text telling the customer how to get free shipping with the product. It names the coupon code to use and
    /// the need to sign in when the promotion requires them.
    /// </param>
    public record ProductFreeShippingPromotionViewModel(string Message);
}
