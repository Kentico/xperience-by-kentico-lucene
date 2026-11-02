using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CMS.ContentEngine;

using DancingGoat.Models;
using DancingGoat.Widgets;

using Kentico.Content.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;

[assembly: RegisterWidget(StorySectionWidgetViewComponent.IDENTIFIER, typeof(StorySectionWidgetViewComponent), "{$dancinggoat.storysectionwidget.title$}", typeof(StorySectionWidgetProperties), Description = "{$dancinggoat.storysectionwidget.description$}", IconClass = "icon-book-opened")]

namespace DancingGoat.Widgets
{
    /// <summary>
    /// View component for Story section widget.
    /// </summary>
    public class StorySectionWidgetViewComponent : ViewComponent
    {
        /// <summary>
        /// Widget identifier.
        /// </summary>
        public const string IDENTIFIER = "DancingGoat.General.StorySectionWidget";


        private readonly IContentRetriever contentRetriever;


        /// <summary>
        /// Creates an instance of <see cref="StorySectionWidgetViewComponent"/> class.
        /// </summary>
        /// <param name="contentRetriever">Content retriever.</param>
        public StorySectionWidgetViewComponent(IContentRetriever contentRetriever)
        {
            this.contentRetriever = contentRetriever;
        }


        public async Task<ViewViewComponentResult> InvokeAsync(StorySectionWidgetProperties properties, CancellationToken cancellationToken)
        {
            var selectedStorySectionGuid = properties.SelectedStorySection.Select(storySectionReference => storySectionReference.Identifier).FirstOrDefault();
            if (selectedStorySectionGuid == default)
            {
                return View("~/Components/Widgets/StorySectionWidget/_StorySectionWidget.cshtml", (StorySectionWidgetViewModel)null);
            }

            var storySections = await contentRetriever.RetrieveContent<StorySection>(
                new RetrieveContentParameters(),
                query => query.Where(where => where.WhereEquals(nameof(IContentQueryDataContainer.ContentItemGUID), selectedStorySectionGuid)).WithAiProvenance(),
                new RetrievalCacheSettings($"WhereEquals_{nameof(IContentQueryDataContainer.ContentItemGUID)}_{selectedStorySectionGuid}_WithAiProvenance"),
                cancellationToken
            );

            var model = StorySectionWidgetViewModel.GetViewModel(storySections.FirstOrDefault());

            return View("~/Components/Widgets/StorySectionWidget/_StorySectionWidget.cshtml", model);
        }
    }
}
