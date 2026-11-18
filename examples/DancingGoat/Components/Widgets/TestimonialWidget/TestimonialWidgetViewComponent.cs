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

[assembly: RegisterWidget(TestimonialWidgetViewComponent.IDENTIFIER, typeof(TestimonialWidgetViewComponent), "{$dancinggoat.testimonialwidget.title$}", typeof(TestimonialWidgetProperties), Description = "{$dancinggoat.testimonialwidget.description$}", IconClass = "icon-right-double-quotation-mark")]

namespace DancingGoat.Widgets
{
    /// <summary>
    /// View component for Testimonial widget.
    /// </summary>
    public class TestimonialWidgetViewComponent : ViewComponent
    {
        /// <summary>
        /// Widget identifier.
        /// </summary>
        public const string IDENTIFIER = "DancingGoat.General.TestimonialWidget";


        private readonly IContentRetriever contentRetriever;


        /// <summary>
        /// Creates an instance of <see cref="TestimonialWidgetViewComponent"/> class.
        /// </summary>
        /// <param name="contentRetriever">Content retriever.</param>
        public TestimonialWidgetViewComponent(IContentRetriever contentRetriever)
        {
            this.contentRetriever = contentRetriever;
        }


        public async Task<ViewViewComponentResult> InvokeAsync(TestimonialWidgetProperties properties, CancellationToken cancellationToken)
        {
            var selectedTestimonialGuid = properties.SelectedTestimonial.Select(testimonialReference => testimonialReference.Identifier).FirstOrDefault();
            if (selectedTestimonialGuid == default)
            {
                return View("~/Components/Widgets/TestimonialWidget/_TestimonialWidget.cshtml", (TestimonialWidgetViewModel)null);
            }

            var testimonials = await contentRetriever.RetrieveContent<Testimonial>(
                new RetrieveContentParameters(),
                query => query.Where(where => where.WhereEquals(nameof(IContentQueryDataContainer.ContentItemGUID), selectedTestimonialGuid)),
                new RetrievalCacheSettings($"WhereEquals_{nameof(IContentQueryDataContainer.ContentItemGUID)}_{selectedTestimonialGuid}"),
                cancellationToken
            );

            var imagePath = await GetImagePath(properties, cancellationToken);

            var model = TestimonialWidgetViewModel.GetViewModel(testimonials.FirstOrDefault(), properties.Heading, imagePath);

            return View("~/Components/Widgets/TestimonialWidget/_TestimonialWidget.cshtml", model);
        }


        private async Task<string> GetImagePath(TestimonialWidgetProperties properties, CancellationToken cancellationToken)
        {
            var imageReference = properties.Image?.FirstOrDefault();
            if (imageReference == null)
            {
                return null;
            }

            var images = await contentRetriever.RetrieveContentByGuids<Image>(
                [imageReference.Identifier],
                cancellationToken
            );

            return images.FirstOrDefault()?.ImageFile.Url;
        }
    }
}
