using CMS.DocumentEngine.Types.DancingGoatCore;

namespace DancingGoat.Models
{
    public class HomeSectionViewModel
    {
        public string Heading { get; set; }
        public string Text { get; set; }
        public string MoreButtonText { get; set; }

        public static HomeSectionViewModel GetViewModel(HomeSection homeSection)
        {
            return homeSection == null ? null : new HomeSectionViewModel
            {
                Heading = homeSection.DocumentName,
                Text = homeSection.Fields.Text,
                MoreButtonText = homeSection.Fields.LinkText,
            };
        }
    }
}