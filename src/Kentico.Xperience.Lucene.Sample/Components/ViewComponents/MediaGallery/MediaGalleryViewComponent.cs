using System.Linq;

using DancingGoat.Models;

using Kentico.Content.Web.Mvc;

using Microsoft.AspNetCore.Mvc;

namespace DancingGoat.ViewComponents
{
    public class MediaGalleryViewComponent : ViewComponent
    {
        private const string MEDIA_LIBRARY_NAME = "CoffeeGallery";

        private readonly MediaFileRepository mediaFileRepository;
        private readonly IMediaFileUrlRetriever fileUrlRetriever;


        public MediaGalleryViewComponent(MediaFileRepository mediaFileRepository, IMediaFileUrlRetriever fileUrlRetriever)
        {
            this.mediaFileRepository = mediaFileRepository;
            this.fileUrlRetriever = fileUrlRetriever;
        }


        public IViewComponentResult Invoke()
        {
            var mediaLibary = mediaFileRepository.GetByName(MEDIA_LIBRARY_NAME);

            if (mediaLibary == null)
            {
                return Content(string.Empty);
            }

            var mediaFiles = mediaFileRepository.GetMediaFiles(MEDIA_LIBRARY_NAME);
            var mediaGallery = new MediaGalleryViewModel(mediaLibary.LibraryDisplayName);
            mediaGallery.MediaFiles = mediaFiles.Select(file => MediaFileViewModel.GetViewModel(file, fileUrlRetriever));

            return View("~/Components/ViewComponents/MediaGallery/Default.cshtml", mediaGallery);
        }
    }
}
