using System.Collections.Generic;

using CMS.Websites;

namespace DancingGoat.Models
{
    public class ContactsIndexViewModel : IWebPageBasedViewModel
    {
        /// <summary>
        /// The company contact data.
        /// </summary>
        public ContactViewModel CompanyContact { get; set; }


        /// <summary>
        /// The company cafes data.
        /// </summary>
        public List<CafeViewModel> CompanyCafes { get; set; }


        /// <summary>
        /// The partner cafes data.
        /// </summary>
        public List<CafeViewModel> PartnerCafes { get; set; }


        /// <inheritdoc/>
        public IWebPageFieldsSource WebPage { get; init; }
    }
}
