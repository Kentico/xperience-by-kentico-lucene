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

[assembly: RegisterPageRoute(Home.CLASS_NAME, typeof(HomeController))]

namespace DancingGoat.Controllers
{
    public class HomeController : Controller
    {
        private readonly IPageDataContextRetriever pageDataContextRetriever;
        private readonly RepositoryCacheHelper repositoryCacheHelper;


        public HomeController(IPageDataContextRetriever pageDataContextRetriever,
            RepositoryCacheHelper repositoryCacheHelper)
        {
            this.pageDataContextRetriever = pageDataContextRetriever;
            this.repositoryCacheHelper = repositoryCacheHelper;
        }


        public async Task<ActionResult> Index(CancellationToken cancellationToken)
        {
            var viewModel = await GetHomeViewModel(cancellationToken);

            return View(viewModel);
        }


        private async Task<HomeIndexViewModel> GetHomeViewModel(CancellationToken cancellationToken)
        {
            var home = pageDataContextRetriever.Retrieve<Home>().Page;

            return await repositoryCacheHelper.CacheData(async cancellationToken =>
            {
                var homeFields = (await home.WithLinkedItems(3, cancellationToken)).Fields;
                var homeSections = homeFields.Sections.OfType<HomeSection>();
                var reference = homeFields.Reference.FirstOrDefault() as Reference;
                var cuppingEvent = homeFields.Event.OfType<Event>().OrderByDescending(e => e.EventDate).FirstOrDefault();

                return new HomeIndexViewModel
                {
                    HomeSections = homeSections.Select(section => HomeSectionViewModel.GetViewModel(section)),
                    Reference = ReferenceViewModel.GetViewModel(reference),
                    Event = EventViewModel.GetViewModel(cuppingEvent)
                };
            },
            cancellationToken,
            $"{nameof(HomeController)}|{nameof(GetHomeViewModel)}",
            () => home.GetLinkedItemsCacheDependencyKeys(3, cancellationToken));
        }
    }
}
