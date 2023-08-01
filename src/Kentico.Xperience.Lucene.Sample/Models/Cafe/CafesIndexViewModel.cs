using System.Collections.Generic;

namespace DancingGoat.Models
{
    public class CafesIndexViewModel
    {
        public IEnumerable<CafeViewModel> CompanyCafes { get; set; }


        public Dictionary<string, List<ContactViewModel>> PartnerCafes { get; set; }
    }
}