using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Mvc.Rendering;

using static DancingGoat.Models.CheckoutFormConstants;

namespace DancingGoat.Models;

public sealed record CustomerAddressViewModel
{
    public CustomerAddressViewModel()
    {
        Countries = new List<SelectListItem>();
        States = new List<SelectListItem>();
    }


    public CustomerAddressViewModel(IEnumerable<SelectListItem> countries)
    {
        Countries = countries;
        States = new List<SelectListItem>();
    }


    [Display(Name = "Street address")]
    [Required(ErrorMessage = REQUIRED_FIELD_ERROR_MESSAGE)]
    [MaxLength(200, ErrorMessage = MAX_LENGTH_ERROR_MESSAGE)]
    public string Line1 { get; set; }

    [Display(Name = "Apartment, suite, unit, etc.")]
    [MaxLength(200, ErrorMessage = MAX_LENGTH_ERROR_MESSAGE)]
    public string Line2 { get; set; }

    [Display(Name = "City")]
    [Required(ErrorMessage = REQUIRED_FIELD_ERROR_MESSAGE)]
    [MaxLength(100, ErrorMessage = MAX_LENGTH_ERROR_MESSAGE)]
    public string City { get; set; }

    [Display(Name = "Postal code")]
    [Required(ErrorMessage = REQUIRED_FIELD_ERROR_MESSAGE)]
    [MaxLength(10, ErrorMessage = MAX_LENGTH_ERROR_MESSAGE)]
    public string PostalCode { get; set; }

    [Display(Name = "Country")]
    [Required(ErrorMessage = REQUIRED_FIELD_ERROR_MESSAGE)]
    public string CountryId { get; set; }

    [Display(Name = "State")]
    public string StateId { get; set; }

    public string Country { get; set; }

    public string State { get; set; }

    public IEnumerable<SelectListItem> Countries { get; set; }

    public IEnumerable<SelectListItem> States { get; set; }
}
