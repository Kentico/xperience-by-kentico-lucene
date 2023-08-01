using System.Linq;
using CMS.DocumentEngine.Types.DancingGoatCore;

using DancingGoat.Models;
using DancingGoat.Widgets;

using Kentico.PageBuilder.Web.Mvc;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;

[assembly: RegisterWidget(CardWidgetViewComponent.IDENTIFIER, typeof(CardWidgetViewComponent), "Card", typeof(CardWidgetProperties), Description = "Displays an image with a centered text.", IconClass = "icon-rectangle-paragraph")]

namespace DancingGoat.Widgets
{
    /// <summary>
    /// Controller for card widget.
    /// </summary>
    public class CardWidgetViewComponent : ViewComponent
    {
        /// <summary>
        /// Widget identifier.
        /// </summary>
        public const string IDENTIFIER = "DancingGoat.LandingPage.CardWidget";


        private readonly MediaRepository mediaRepository;

        /// <summary>
        /// Creates an instance of <see cref="CardWidgetViewComponent"/> class.
        /// </summary>
        /// <param name="mediaRepository">Repository for media files.</param>
        public CardWidgetViewComponent(MediaRepository mediaRepository)
        {
            this.mediaRepository = mediaRepository;
        }


        public ViewViewComponentResult Invoke(CardWidgetProperties properties)
        {
            var image = GetImage(properties);

            return View("~/Components/Widgets/CardWidget/_CardWidget.cshtml", new CardWidgetViewModel
            {
                ImagePath = image?.Fields.File.Url,
                Text = properties.Text
            });
        }


        private Media GetImage(CardWidgetProperties properties)
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