using System.Linq;

using CMS.DocumentEngine.Types.DancingGoatCore;

using DancingGoat.Models;
using DancingGoat.Widgets;

using Kentico.PageBuilder.Web.Mvc;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;

[assembly: RegisterWidget(BannerWidgetViewComponent.IDENTIFIER, typeof(BannerWidgetViewComponent), "Banner", typeof(BannerWidgetProperties), Description = "Displays the text and image.", IconClass = "icon-ribbon")]

namespace DancingGoat.Widgets
{
    /// <summary>
    /// Banner widget service.
    /// </summary>
    public class BannerWidgetViewComponent : ViewComponent
    {
        /// <summary>
        /// Widget identifier.
        /// </summary>
        public const string IDENTIFIER = "DancingGoat.HomePage.BannerWidget";


        private readonly MediaRepository mediaRepository;


        /// <summary>
        /// Initializes a new instance of the <see cref="BannerWidgetViewComponent"/> class.
        /// </summary>
        /// <param name="mediaRepository">Repository for media files.</param>
        public BannerWidgetViewComponent(MediaRepository mediaRepository)
        {
            this.mediaRepository = mediaRepository;
        }


        public ViewViewComponentResult Invoke(BannerWidgetProperties properties)
        {
            var image = GetImage(properties);

            return View("~/Components/Widgets/BannerWidget/_BannerWidget.cshtml", new BannerWidgetModel
            {
                ImagePath = image?.Fields.File.Url,
                Text = properties.Text,
                LinkUrl = properties.LinkUrl,
                LinkTitle = properties.LinkTitle
            });
        }


        private Media GetImage(BannerWidgetProperties properties)
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