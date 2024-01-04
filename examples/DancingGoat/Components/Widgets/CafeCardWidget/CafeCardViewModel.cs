using System.Linq;

using CMS.DocumentEngine.Types.DancingGoatCore;

namespace DancingGoat.Widgets
{
    /// <summary>
    /// View model for Cafe card widget.
    /// </summary>
    public class CafeCardViewModel
    {
        /// <summary>
        /// Cafe name.
        /// </summary>
        public string Name { get; set; }


        /// <summary>
        /// Cafe background image path.
        /// </summary>
        public string PhotoPath { get; set; }


        /// <summary>
        /// Cafe background image short description.
        /// </summary>
        public string PhotoShortDescription { get; set; }


        /// <summary>
        /// Gets ViewModel for <paramref name="cafe"/>.
        /// </summary>
        /// <param name="cafe">Cafe.</param>
        /// <returns>Hydrated view model.</returns>
        public static CafeCardViewModel GetViewModel(Cafe cafe)
        {
            var cafeMedia = cafe?.Fields.Photo.FirstOrDefault() as Media;
            return cafe == null
                ? new CafeCardViewModel()
                : new CafeCardViewModel
                {
                    Name = cafe.DocumentName,
                    PhotoPath = cafeMedia?.Fields.File.Url,
                    PhotoShortDescription = cafeMedia?.Fields.ShortDescription ?? string.Empty
                };
        }
    }
}