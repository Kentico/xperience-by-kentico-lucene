using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

namespace DancingGoat.ViewComponents
{
    /// <summary>
    /// View component rendering SEO metadata (title, meta description, robots, canonical URL, OpenGraph tags
    /// and JSON-LD structured data) into the page head.
    /// </summary>
    public sealed class SeoMetadataViewComponent : ViewComponent
    {
        private readonly SeoMetadataService seoMetadataService;


        /// <summary>
        /// Initializes a new instance of the <see cref="SeoMetadataViewComponent"/> class.
        /// </summary>
        public SeoMetadataViewComponent(SeoMetadataService seoMetadataService)
        {
            this.seoMetadataService = seoMetadataService;
        }


        /// <summary>
        /// Renders the SEO metadata for the given page view model.
        /// </summary>
        /// <param name="pageModel">View model of the rendered page.</param>
        /// <param name="pageTitle">Title used when the view model does not provide an SEO title.</param>
        public async Task<IViewComponentResult> InvokeAsync(object pageModel, string pageTitle)
        {
            var baseUri = new Uri($"{Request.Scheme}://{Request.Host}");
            var model = await seoMetadataService.GetSeoMetadata(pageModel, pageTitle, baseUri, HttpContext.RequestAborted);

            return View("~/Components/ViewComponents/SeoMetadata/Default.cshtml", model);
        }
    }
}
