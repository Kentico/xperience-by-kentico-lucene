using System.Collections.Generic;

namespace DancingGoat.Models
{
    public class HomeIndexViewModel
    {
        public IEnumerable<HomeSectionViewModel> HomeSections { get; set; }

        public ReferenceViewModel Reference { get; set; }

        public EventViewModel Event { get; set; }
    }
}