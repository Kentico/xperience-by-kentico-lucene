using System.Collections.Generic;

namespace DancingGoat.Models
{
    public class ContactsIndexViewModel
    {
        /// <summary>
        /// The company contact data.
        /// </summary>
        public ContactViewModel CompanyContact { get; set; }


        /// <summary>
        /// The company cafes data.
        /// </summary>
        public List<CafeViewModel> CompanyCafes { get; set; }
    }
}