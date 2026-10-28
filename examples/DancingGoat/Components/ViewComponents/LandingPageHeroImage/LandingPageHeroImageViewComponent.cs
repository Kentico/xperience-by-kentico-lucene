using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using CMS.ContentEngine;

using DancingGoat.Models;

using Kentico.Content.Web.Mvc;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;

namespace DancingGoat.ViewComponents
{
    /// <summary>
    /// Renders the background image of the landing page hero.
    /// </summary>
    /// <remarks>
    /// Exists so the page template view does not retrieve content itself — data access belongs
    /// in a view component, as with the Page Builder widgets.
    /// </remarks>
    public class LandingPageHeroImageViewComponent : ViewComponent
    {
        private readonly IContentRetriever contentRetriever;


        /// <summary>
        /// Creates an instance of the <see cref="LandingPageHeroImageViewComponent"/> class.
        /// </summary>
        /// <param name="contentRetriever">Content retriever.</param>
        public LandingPageHeroImageViewComponent(IContentRetriever contentRetriever)
        {
            this.contentRetriever = contentRetriever;
        }


        /// <summary>
        /// Resolves the referenced image and renders it.
        /// </summary>
        /// <param name="heroImage">Content item reference to the hero image.</param>
        public async Task<ViewViewComponentResult> InvokeAsync(IEnumerable<ContentItemReference> heroImage)
        {
            var reference = heroImage?.FirstOrDefault();
            if (reference == null)
            {
                return View("~/Components/ViewComponents/LandingPageHeroImage/Default.cshtml", (string)null);
            }

            var images = await contentRetriever.RetrieveContentByGuids<Image>(
                [reference.Identifier],
                HttpContext.RequestAborted
            );

            return View("~/Components/ViewComponents/LandingPageHeroImage/Default.cshtml", images.FirstOrDefault()?.ImageFile.Url);
        }
    }
}
