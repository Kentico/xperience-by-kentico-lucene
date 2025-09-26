namespace DancingGoat.Models;

/// <summary>
/// Data transfer object for customer information in the checkout process.
/// </summary>
public sealed record CustomerDto
{
    public string FirstName { get; set; }

    public string LastName { get; set; }

    public string Email { get; set; }

    public string PhoneNumber { get; set; }

    public string Company { get; set; }

    public string BillingAddressLine1 { get; set; }

    public string BillingAddressLine2 { get; set; }

    public string BillingAddressCity { get; set; }

    public string BillingAddressPostalCode { get; set; }

    public int BillingAddressCountryId { get; set; }

    public int BillingAddressStateId { get; set; }

    public string ShippingAddressLine1 { get; set; }

    public string ShippingAddressLine2 { get; set; }

    public string ShippingAddressCity { get; set; }

    public string ShippingAddressPostalCode { get; set; }

    public int ShippingAddressCountryId { get; set; }

    public int ShippingAddressStateId { get; set; }
}
