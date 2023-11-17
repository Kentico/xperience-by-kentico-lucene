using Microsoft.Extensions.Localization;

namespace DancingGoat.Models
{
    public class ContactViewModel
    {
        public string Name { get; set; }


        public string Phone { get; set; }


        public string Email { get; set; }


        public string ZIP { get; set; }


        public string Street { get; set; }


        public string City { get; set; }


        public string Country { get; set; }


        public ContactViewModel()
        {
        }


        public ContactViewModel(IContact contact)
        {
            Name = contact.Name;
            Phone = contact.Phone;
            Email = contact.Email;
            ZIP = contact.ZIP;
            Street = contact.Street;
            City = contact.City;
            Country = contact.Country;
        }


        public static ContactViewModel GetViewModel(IContact contact, IStringLocalizer localizer)
        {
            return new ContactViewModel(contact);
        }
    }
}