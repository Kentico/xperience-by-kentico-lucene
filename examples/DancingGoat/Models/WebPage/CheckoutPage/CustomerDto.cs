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

    public string AddressLine1 { get; set; }

    public string AddressLine2 { get; set; }

    public string AddressCity { get; set; }

    public string AddressPostalCode { get; set; }

    public int AddressCountryId { get; set; }

    public int AddressStateId { get; set; }
}
