using System.Linq;
using System.Threading.Tasks;

using DancingGoat.Models;
using DancingGoat.Widgets;

using Kentico.Content.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;

[assembly: RegisterWidget(HeroImageWidgetViewComponent.IDENTIFIER, typeof(HeroImageWidgetViewComponent), "{$dancinggoat.heroimagewidget.title$}", typeof(HeroImageWidgetProperties), Description = "{$dancinggoat.heroimagewidget.description$}", IconClass = "icon-badge")]

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

        private readonly IContentRetriever contentRetriever;


        /// <summary>
        /// Creates an instance of <see cref="HeroImageWidgetViewComponent"/> class.
        /// </summary>
        /// <param name="contentRetriever">Content retriever.</param>
        public HeroImageWidgetViewComponent(IContentRetriever contentRetriever)
        {
            this.contentRetriever = contentRetriever;
        }


        public async Task<ViewViewComponentResult> InvokeAsync(HeroImageWidgetProperties properties)
        {
            var image = await GetImage(properties);

            return View("~/Components/Widgets/HeroImageWidget/_HeroImageWidget.cshtml", new HeroImageWidgetViewModel
            {
                ImagePath = image?.ImageFile.Url,
                Text = properties.Text,
                ButtonText = properties.ButtonText,
                ButtonTarget = properties.ButtonTarget,
                Theme = properties.Theme
            });
        }


        private async Task<Image> GetImage(HeroImageWidgetProperties properties)
        {
            var image = properties.Image.FirstOrDefault();

            if (image == null)
            {
                return null;
            }

            var result = await contentRetriever.RetrieveContentByGuids<Image>(
                [image.Identifier],
                HttpContext.RequestAborted
            );

            return result.FirstOrDefault();
        }
    }
}
