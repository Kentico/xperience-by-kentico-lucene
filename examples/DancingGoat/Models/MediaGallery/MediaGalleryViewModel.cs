using System.Collections.Generic;

namespace DancingGoat.Models
{
    public class MediaGalleryViewModel
    {
        public string MediaGalleryName { get; set; }
        public IEnumerable<MediaFileViewModel> MediaFiles { get; set; } = new List<MediaFileViewModel>();


        public MediaGalleryViewModel(string mediaGalleryName)
        {
            MediaGalleryName = mediaGalleryName;
        }
    }
}