using System.Linq;
using System.Threading.Tasks;

using DancingGoat.Models;
using DancingGoat.Widgets;

using Kentico.Content.Web.Mvc.Routing;
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

        private readonly ImageRepository imageRepository;
        private readonly IPreferredLanguageRetriever currentLanguageRetriever;


        /// <summary>
        /// Creates an instance of <see cref="HeroImageWidgetViewComponent"/> class.
        /// </summary>
        /// <param name="imageRepository">Repository for images.</param>
        /// <param name="currentLanguageRetriever">Retrieves preferred language name for the current request. Takes language fallback into account.</param>
        public HeroImageWidgetViewComponent(ImageRepository imageRepository, IPreferredLanguageRetriever currentLanguageRetriever)
        {
            this.imageRepository = imageRepository;
            this.currentLanguageRetriever = currentLanguageRetriever;
        }


        public async Task<ViewViewComponentResult> InvokeAsync(HeroImageWidgetProperties properties)
        {
            var languageName = currentLanguageRetriever.Get();
            var image = await GetImage(properties, languageName);

            return View("~/Components/Widgets/HeroImageWidget/_HeroImageWidget.cshtml", new HeroImageWidgetViewModel
            {
                ImagePath = image?.ImageFile.Url,
                Text = properties.Text,
                ButtonText = properties.ButtonText,
                ButtonTarget = properties.ButtonTarget,
                Theme = properties.Theme
            });
        }


        private async Task<Image> GetImage(HeroImageWidgetProperties properties, string languageName)
        {
            var image = properties.Image.FirstOrDefault();

            if (image == null)
            {
                return null;
            }

            return await imageRepository.GetImage(image.Identifier, languageName);
        }
    }
}
