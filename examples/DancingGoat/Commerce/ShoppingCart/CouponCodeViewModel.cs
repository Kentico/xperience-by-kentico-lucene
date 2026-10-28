namespace DancingGoat.Commerce;

/// <summary>
/// Represents a coupon code and its current status in the shopping cart context.
/// </summary>
public sealed class CouponCodeViewModel
{
    /// <summary>
    /// Gets or sets the coupon code string.
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// Gets or sets the status of the coupon code, indicating whether it has been applied,
    /// is applicable, or is not applicable in the current context.
    /// </summary>
    public CouponCodeStatus Status { get; set; }
}
