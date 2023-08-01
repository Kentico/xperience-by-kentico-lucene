using System.Collections.Generic;
using System.Threading.Tasks;

using DancingGoat.Models;

using Microsoft.AspNetCore.Mvc;

namespace DancingGoat.ViewComponents
{
    public class SocialLinksViewComponent : ViewComponent
    {
        private readonly SocialLinkRepository socialLinkRepository;

        public SocialLinksViewComponent(SocialLinkRepository socialLinkRepository)
        {
            this.socialLinkRepository = socialLinkRepository;
        }


        public async Task<IViewComponentResult> InvokeAsync()
        {
            var socialLinks = await socialLinkRepository.GetSocialLinks(HttpContext.RequestAborted);
            var socialLinksModel = new List<SocialLinkViewModel>();
            
            foreach (var link in socialLinks)
            {
                socialLinksModel.Add(SocialLinkViewModel.GetViewModel(link));
            }

            return View("~/Components/ViewComponents/SocialLinks/Default.cshtml", socialLinksModel);
        }
    }
}
