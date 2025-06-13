using System.ComponentModel.DataAnnotations;

using static DancingGoat.Models.CheckoutFormConstants;

namespace DancingGoat.Models;

public sealed record CustomerViewModel
{
    public CustomerViewModel()
    {
        FirstName = LastName = Email = PhoneNumber = string.Empty;
    }


    public CustomerViewModel(string firstName, string lastName, string email, string phoneNumber)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        PhoneNumber = phoneNumber;
    }


    [Display(Name = "First name")]
    [Required(ErrorMessage = REQUIRED_FIELD_ERROR_MESSAGE)]
    [MaxLength(100, ErrorMessage = MAX_LENGTH_ERROR_MESSAGE)]
    public string FirstName { get; set; }

    [Display(Name = "Last name")]
    [Required(ErrorMessage = REQUIRED_FIELD_ERROR_MESSAGE)]
    [MaxLength(200, ErrorMessage = MAX_LENGTH_ERROR_MESSAGE)]
    public string LastName { get; set; }

    [Display(Name = "Company")]
    [MaxLength(200, ErrorMessage = MAX_LENGTH_ERROR_MESSAGE)]
    public string Company { get; set; }

    [Display(Name = "Email")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [Required(ErrorMessage = REQUIRED_FIELD_ERROR_MESSAGE)]
    [MaxLength(255, ErrorMessage = MAX_LENGTH_ERROR_MESSAGE)]
    public string Email { get; set; }

    [Display(Name = "Phone")]
    [Phone(ErrorMessage = "Please enter a valid phone number.")]
    [MaxLength(30, ErrorMessage = MAX_LENGTH_ERROR_MESSAGE)]
    public string PhoneNumber { get; set; }


    public bool IsEmpty()
    {
        return string.IsNullOrEmpty(FirstName) && string.IsNullOrEmpty(LastName) && string.IsNullOrEmpty(Company) && string.IsNullOrEmpty(Email) && string.IsNullOrEmpty(PhoneNumber);
    }


    public CustomerDto ToCustomerDto(CustomerAddressViewModel customerAddressViewModel)
    {
        int.TryParse(customerAddressViewModel.CountryId, out int countryId);
        int.TryParse(customerAddressViewModel.StateId, out int stateId);

        return new CustomerDto
        {
            FirstName = FirstName,
            LastName = LastName,
            Email = Email,
            PhoneNumber = PhoneNumber,

            Company = Company,
            AddressLine1 = customerAddressViewModel.Line1,
            AddressLine2 = customerAddressViewModel.Line2,
            AddressCity = customerAddressViewModel.City,
            AddressPostalCode = customerAddressViewModel.PostalCode,
            AddressCountryId = countryId,
            AddressStateId = stateId
        };
    }
}
