using System;
using System.Collections.Generic;

using CMS.ContactManagement;
using CMS.DataEngine;

namespace DancingGoat.Helpers.Generator
{
    /// <summary>
    /// Contains methods for generating sample contacts for online marketing demonstrations.
    /// </summary>
    public class ContactsGenerator
    {
        private readonly IInfoProvider<ContactInfo> contactInfoProvider;

        private static readonly string[] FirstNames =
        [
            "Emma", "Liam", "Olivia", "Noah", "Ava", "Ethan", "Sophia", "Mason", "Isabella", "James",
            "Charlotte", "Oliver", "Amelia", "Elijah", "Mia", "William", "Harper", "Benjamin", "Evelyn", "Lucas",
            "Abigail", "Henry", "Emily", "Alexander", "Elizabeth", "Michael", "Sofia", "Daniel", "Avery", "Matthew"
        ];

        private static readonly string[] LastNames =
        [
            "Johnson", "Smith", "Williams", "Brown", "Jones", "Garcia", "Martinez", "Rodriguez", "Wilson", "Anderson",
            "Taylor", "Thomas", "Moore", "Jackson", "Martin", "Lee", "Thompson", "White", "Harris", "Clark",
            "Lewis", "Robinson", "Walker", "Hall", "Allen", "Young", "King", "Wright", "Lopez", "Hill"
        ];

        private static readonly string[] Companies =
        [
            "Acme Corp", "Tech Solutions", "Digital Marketing Inc", "Consulting Group", "Retail Chain",
            "Manufacturing Co", "Design Studio", "Finance Partners", "Healthcare Services", "Education Hub",
            "Global Enterprises", "Innovation Labs", "Strategic Advisors", "Creative Agency", "Data Systems",
            "Cloud Services", "Marketing Pro", "Business Solutions", "Development Group", "Analytics Partners"
        ];


        /// <summary>
        /// Initializes a new instance of the <see cref="ContactsGenerator"/> class.
        /// </summary>
        public ContactsGenerator(IInfoProvider<ContactInfo> contactInfoProvider)
        {
            this.contactInfoProvider = contactInfoProvider;
        }


        /// <summary>
        /// Generates sample contacts. Suitable only for Dancing Goat demo site.
        /// </summary>
        /// <param name="count">Number of contacts to generate.</param>
        public void Generate(int count = DigitalMarketingGeneratorConstants.DEFAULT_CONTACTS_COUNT)
        {
            CreateSampleContacts(count);
        }


        private void CreateSampleContacts(int count)
        {
            // nosemgrep: dotnet.cryptography.CRYPTO-using-math-random - approved, not for crypto purposes.
            var random = new Random();
            var contacts = new List<ContactInfo>();

            for (int i = 1; i <= count; i++)
            {
                var firstName = FirstNames[random.Next(FirstNames.Length)];
                var lastName = LastNames[random.Next(LastNames.Length)];
                var company = Companies[random.Next(Companies.Length)];
                var email = $"{firstName.ToLowerInvariant()}.{lastName.ToLowerInvariant()}{i}{DigitalMarketingGeneratorConstants.SAMPLE_CONTACTS_EMAIL_DOMAIN}";

                var newContact = new ContactInfo()
                {
                    ContactFirstName = firstName,
                    ContactLastName = lastName,
                    ContactEmail = email,
                    ContactCompanyName = company,
                    ContactMonitored = true,
                    ContactCreated = DateTime.Now.AddDays(-random.Next(1, 90))
                };

                contacts.Add(newContact);
            }

            contactInfoProvider.BulkInsert(contacts);
        }
    }
}
