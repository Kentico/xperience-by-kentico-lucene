using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Mvc.Rendering;

using static DancingGoat.Models.CheckoutFormConstants;

namespace DancingGoat.Models;

public sealed record PaymentShippingViewModel
{
    public PaymentShippingViewModel()
    {
        PaymentMethod = ShippingMethod = string.Empty;
    }


    public string PaymentMethod { get; set; }

    public string ShippingMethod { get; set; }

    public decimal ShippingPrice { get; set; }

    [Display(Name = "Payment method")]
    [Required(ErrorMessage = REQUIRED_FIELD_ERROR_MESSAGE)]
    public string PaymentMethodId { get; set; }

    [Display(Name = "Shipping method")]
    [Required(ErrorMessage = REQUIRED_FIELD_ERROR_MESSAGE)]
    public string ShippingMethodId { get; set; }

    public IEnumerable<SelectListItem> Payments { get; set; }

    public IEnumerable<SelectListItem> Shippings { get; set; }

    public bool IsEmpty()
    {
        return string.IsNullOrEmpty(PaymentMethod) && string.IsNullOrEmpty(ShippingMethod);
    }
}
