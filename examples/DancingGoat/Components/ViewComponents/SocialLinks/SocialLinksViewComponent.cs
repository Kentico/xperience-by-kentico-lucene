using System.Linq;
using System.Threading.Tasks;

using DancingGoat.Models;

using Kentico.Content.Web.Mvc;

using Microsoft.AspNetCore.Mvc;

namespace DancingGoat.ViewComponents
{
    public class SocialLinksViewComponent : ViewComponent
    {
        private readonly IContentRetriever contentRetriever;


        public SocialLinksViewComponent(IContentRetriever contentRetriever)
        {
            this.contentRetriever = contentRetriever;
        }


        public async Task<IViewComponentResult> InvokeAsync()
        {
            var socialLinks = await contentRetriever.RetrieveContent<SocialLink>(
                new RetrieveContentParameters { LinkedItemsMaxLevel = 1 },
                HttpContext.RequestAborted
            );

            return View("~/Components/ViewComponents/SocialLinks/Default.cshtml", socialLinks.Select(SocialLinkViewModel.GetViewModel));
        }
    }
}
