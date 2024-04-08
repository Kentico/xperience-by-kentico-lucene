namespace DancingGoat.Models
{
    public record ContactViewModel(string Name, string Street, string City, string Country, string ZipCode, string Phone, string Email)
    {
        /// <summary>
        /// Validates and maps <see cref="Contact"/> to a <see cref="ContactViewModel"/>.
        /// </summary>
        public static ContactViewModel GetViewModel(Contact contact)
        {
            if (contact is null)
            {
                return null;
            }

            return new ContactViewModel(
                contact.ContactName,
                contact.ContactStreet,
                contact.ContactCity,
                contact.ContactCountry,
                contact.ContactZipCode,
                contact.ContactPhone,
                contact.ContactEmail
            );
        }
    }
}
