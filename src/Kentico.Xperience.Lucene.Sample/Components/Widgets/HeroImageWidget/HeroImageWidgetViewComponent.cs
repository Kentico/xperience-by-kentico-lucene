using System.Linq;

using CMS.DocumentEngine.Types.DancingGoatCore;

using DancingGoat.Models;
using DancingGoat.Widgets;

using Kentico.PageBuilder.Web.Mvc;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;

[assembly: RegisterWidget(HeroImageWidgetViewComponent.IDENTIFIER, typeof(HeroImageWidgetViewComponent), "Hero image", typeof(HeroImageWidgetProperties), Description = "Displays an image, text, and a CTA button.", IconClass = "icon-badge")]

namespace DancingGoat.Widgets
{
    /// <summary>
    /// Controller for hero image widget.
    /// </summary>
    public class HeroImageWidgetViewComponent : ViewComponent
    {
        /// <summary>
        /// Widget identifier.
        /// </summary>
        public const string IDENTIFIER = "DancingGoat.LandingPage.HeroImage";


        private readonly MediaRepository mediaRepository;

        /// <summary>
        /// Creates an instance of <see cref="HeroImageWidgetViewComponent"/> class.
        /// </summary>
        /// <param name="mediaRepository">Repository for media files.</param>
        public HeroImageWidgetViewComponent(MediaRepository mediaRepository)
        {
            this.mediaRepository = mediaRepository;
        }


        public ViewViewComponentResult Invoke(HeroImageWidgetProperties properties)
        {
            var image = GetImage(properties);

            return View("~/Components/Widgets/HeroImageWidget/_HeroImageWidget.cshtml", new HeroImageWidgetViewModel
            {
                ImagePath = image?.Fields.File.Url,
                Text = properties.Text,
                ButtonText = properties.ButtonText,
                ButtonTarget = properties.ButtonTarget,
                Theme = properties.Theme
            });
        }

        private Media GetImage(HeroImageWidgetProperties properties)
        {
            var image = properties.Image.FirstOrDefault();

            if (image == null)
            {
                return null;
            }

            return mediaRepository.GetMediaFile(image.ItemId);
        }
    }
}