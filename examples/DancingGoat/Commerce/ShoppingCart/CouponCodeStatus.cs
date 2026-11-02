namespace DancingGoat.Commerce
{
    /// <summary>
    /// Represents the result of evaluating a coupon code within the current context
    /// (such as the shopping cart, customer, and active promotions).
    /// </summary>
    public enum CouponCodeStatus
    {
        /// <summary>
        /// The coupon code exists and has been successfully applied.
        /// </summary>
        Applied,

        /// <summary>
        /// The coupon code exists and is valid in the current context,
        /// but has not been applied because another coupon providing a higher
        /// discount was applied instead.
        /// </summary>
        Applicable,

        /// <summary>
        /// The coupon code exists, but cannot be applied in the current context
        /// due to failing validation or business rules.
        /// </summary>
        NotApplicable,
    }
}
