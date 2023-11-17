using System.Linq;

using CMS.DocumentEngine.Types.DancingGoatCore;

namespace DancingGoat.Models
{
    public class AboutUsSectionViewModel
    {
        public string Heading { get; set; }
        

        public string Text { get; set; }


        public string ImagePath { get; set; }


        public static AboutUsSectionViewModel GetViewModel(AboutUsSection aboutUsSection)
        {
            return new AboutUsSectionViewModel
            {
                Heading = aboutUsSection.DocumentName,
                Text = aboutUsSection.Fields.Text,
                ImagePath = (aboutUsSection.Fields.Image.FirstOrDefault() as Media)?.Fields.File?.Url
            };
        }
    }
}