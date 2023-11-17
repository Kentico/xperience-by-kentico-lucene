using System.Linq;

using CMS.DocumentEngine.Types.DancingGoatCore;

using Microsoft.Extensions.Localization;

namespace DancingGoat.Models
{
    public class CafeViewModel
    {
        public string PhotoPath { get; set; }


        public string Note { get; set; }


        public ContactViewModel Contact { get; set; }


        public static CafeViewModel GetViewModel(Cafe cafe, IStringLocalizer<SharedResources> localizer)
        {
            return new CafeViewModel
            {
                PhotoPath = (cafe.Fields.Photo.FirstOrDefault() as Media)?.Fields.File?.Url,
                Note = cafe.Fields.AdditionalNotes,
                Contact = ContactViewModel.GetViewModel(cafe, localizer)
            };
        }
    }
}