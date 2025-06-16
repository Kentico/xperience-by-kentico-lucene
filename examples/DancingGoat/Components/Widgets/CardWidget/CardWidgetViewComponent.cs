using System.Linq;
using System.Threading.Tasks;

using DancingGoat.Models;
using DancingGoat.Widgets;

using Kentico.Content.Web.Mvc;
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


        private readonly IContentRetriever contentRetriever;


        /// <summary>
        /// Creates an instance of <see cref="CardWidgetViewComponent"/> class.
        /// </summary>
        /// <param name="contentRetriever">Content retriever.</param>
        public CardWidgetViewComponent(IContentRetriever contentRetriever)
        {
            this.contentRetriever = contentRetriever;
        }


        public async Task<ViewViewComponentResult> InvokeAsync(CardWidgetProperties properties)
        {
            var image = await GetImage(properties);

            return View("~/Components/Widgets/CardWidget/_CardWidget.cshtml", new CardWidgetViewModel
            {
                ImagePath = image?.ImageFile.Url,
                Text = properties.Text
            });
        }


        private async Task<Image> GetImage(CardWidgetProperties properties)
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
