using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CMS.DocumentEngine;
using CMS.DocumentEngine.Types.DancingGoatCore;

using DancingGoat.Controllers;
using DancingGoat.Infrastructure;
using DancingGoat.Models;

using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;

using Microsoft.AspNetCore.Mvc;

[assembly: RegisterPageRoute(AboutUs.CLASS_NAME, typeof(AboutController))]

namespace DancingGoat.Controllers
{
    public class AboutController : Controller
    {
        private readonly IPageDataContextRetriever pageDataContextRetriever;
        private readonly RepositoryCacheHelper repositoryCacheHelper;


        public AboutController(IPageDataContextRetriever pageDataContextRetriever,
            RepositoryCacheHelper repositoryCacheHelper)
        {
            this.pageDataContextRetriever = pageDataContextRetriever;
            this.repositoryCacheHelper = repositoryCacheHelper;
        }


        public async Task<ActionResult> Index(CancellationToken cancellationToken)
        {
            var viewModel = await GetAboutUsViewModel(cancellationToken);

            return View(viewModel);
        }


        private async Task<AboutUsViewModel> GetAboutUsViewModel(CancellationToken cancellationToken)
        {
            var aboutUs = pageDataContextRetriever.Retrieve<AboutUs>().Page;

            return await repositoryCacheHelper.CacheData(async cancellationToken =>
            {
                var aboutUsFields = (await aboutUs.WithLinkedItems(1, cancellationToken)).Fields;
                var sections = GetSections(aboutUsFields.Sections.OfType<AboutUsSection>());
                var reference = aboutUsFields.Reference.FirstOrDefault() as Reference;

                return new AboutUsViewModel
                {
                    Sections = sections,
                    Reference = ReferenceViewModel.GetViewModel(reference)
                };
            },
            cancellationToken,
            $"{nameof(AboutController)}|{nameof(GetAboutUsViewModel)}",
            () => aboutUs.GetLinkedItemsCacheDependencyKeys(3, cancellationToken));
        }


        private List<AboutUsSectionViewModel> GetSections(IEnumerable<AboutUsSection> sideStories)
        {
            var sections = new List<AboutUsSectionViewModel>();

            foreach (var sideStory in sideStories)
            {
                var section = AboutUsSectionViewModel.GetViewModel(sideStory);
                sections.Add(section);
            }

            return sections;
        }
    }
}