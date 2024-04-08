using System.Linq;

namespace DancingGoat.Models
{
    public record ReferenceViewModel(string Name, string Description, string Text, string ImageUrl, string ImageShortDescription)
    {
        /// <summary>
        /// Validates and maps <see cref="Reference"/> to a <see cref="ReferenceViewModel"/>.
        /// </summary>
        public static ReferenceViewModel GetViewModel(Reference reference)
        {
            if (reference == null)
            {
                return null;
            }

            return new ReferenceViewModel(
                reference.ReferenceName,
                reference.ReferenceDescription,
                reference.ReferenceText,
                reference.ReferenceImage.FirstOrDefault()?.ImageFile.Url,
                reference.ReferenceImage.FirstOrDefault()?.ImageShortDescription
             );
        }
    }
}
