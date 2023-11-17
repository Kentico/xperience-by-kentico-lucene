using DancingGoat.Models;

namespace CMS.DocumentEngine.Types.DancingGoatCore
{
    /// <summary>
    /// Specification of Cafe members and IContact interface relationship.
    /// </summary>
    public partial class Cafe : IContact
    {
        public string Name
        {
            get
            {
                return DocumentName;
            }
        }


        public string Phone
        {
            get
            {
                return Fields.Phone;
            }
        }


        public string Email
        {
            get
            {
                return "";
            }
        }


        public string ZIP
        {
            get
            {
                return Fields.ZipCode;
            }
        }


        public string Street
        {
            get
            {
                return Fields.Street;
            }
        }


        public string City
        {
            get
            {
                return Fields.City;
            }
        }


        public string Country
        {
            get
            {
                return Fields.Country;
            }
        }
    }
}