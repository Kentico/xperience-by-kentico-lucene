using System.Linq;
using System.Threading.Tasks;

using DancingGoat.Models;

using Kentico.Content.Web.Mvc.Routing;

using Microsoft.AspNetCore.Mvc;

namespace DancingGoat.ViewComponents
{
    public class SocialLinksViewComponent : ViewComponent
    {
        private readonly SocialLinkRepository socialLinkRepository;
        private readonly IPreferredLanguageRetriever currentLanguageRetriever;

        public SocialLinksViewComponent(SocialLinkRepository socialLinkRepository, IPreferredLanguageRetriever currentLanguageRetriever)
        {
            this.socialLinkRepository = socialLinkRepository;
            this.currentLanguageRetriever = currentLanguageRetriever;
        }


        public async Task<IViewComponentResult> InvokeAsync()
        {
            var languageName = currentLanguageRetriever.Get();

            var socialLinks = await socialLinkRepository.GetSocialLinks(languageName, HttpContext.RequestAborted);

            return View("~/Components/ViewComponents/SocialLinks/Default.cshtml", socialLinks.Select(SocialLinkViewModel.GetViewModel));
        }
    }
}
