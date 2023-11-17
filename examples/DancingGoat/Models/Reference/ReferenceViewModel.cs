using System.Linq;

using CMS.DocumentEngine.Types.DancingGoatCore;

namespace DancingGoat.Models
{
    public class ReferenceViewModel
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public string Text { get; set; }

        public string ImagePath { get; set; }

        public string ImageShortDescription { get; set; }


        public static ReferenceViewModel GetViewModel(Reference reference)
        {
            if (reference == null)
            {
                return null;
            }

            var image = reference.Fields.Image.FirstOrDefault() as Media;

            return new ReferenceViewModel
            {
                Name = reference.DocumentName,
                Description = reference.ReferenceDescription,
                Text = reference.ReferenceText,
                ImagePath = image?.Fields.File?.Url,
                ImageShortDescription = image?.Fields.ShortDescription ?? string.Empty
            };
        }
    }
}