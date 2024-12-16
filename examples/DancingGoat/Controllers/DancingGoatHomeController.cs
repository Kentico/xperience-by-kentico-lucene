using System.Linq;
using System.Threading.Tasks;

using CMS.ContentEngine;
using CMS.Helpers;

using DancingGoat;
using DancingGoat.Controllers;
using DancingGoat.Models;

using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;

using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWebPageRoute(HomePage.CONTENT_TYPE_NAME, typeof(DancingGoatHomeController), WebsiteChannelNames = new[] { DancingGoatConstants.WEBSITE_CHANNEL_NAME })]

namespace DancingGoat.Controllers
{
    public class DancingGoatHomeController : Controller
    {
        private readonly HomePageRepository homePageRepository;
        private readonly IWebPageDataContextRetriever webPageDataContextRetriever;
        private readonly IContentQueryExecutor executor;
        private readonly IProgressiveCache progressiveCache;

        public DancingGoatHomeController(HomePageRepository homePageRepository, IWebPageDataContextRetriever webPageDataContextRetriever, IContentQueryExecutor executor, IProgressiveCache progressiveCache)
        {
            this.homePageRepository = homePageRepository;
            this.webPageDataContextRetriever = webPageDataContextRetriever;
            this.executor = executor;
            this.progressiveCache = progressiveCache;
        }

        public async Task<IActionResult> Index()
        {
            var webPage = webPageDataContextRetriever.Retrieve().WebPage;

            var homePage = await homePageRepository.GetHomePage(webPage.WebPageItemID, webPage.LanguageName, HttpContext.RequestAborted);

            var cafes = await progressiveCache.LoadAsync(async (settings, cancellationToken) =>
            {
                var builder = new ContentItemQueryBuilder();
                builder.ForContentTypes(p =>
                    {
                        p.InSmartFolder(homePage.HomePageCafesFolder.Identifier)
                            .WithContentTypeFields()
                            .OfContentType(Cafe.CONTENT_TYPE_NAME)
                            .WithLinkedItems(1);
                    })
                    .InLanguage(webPage.LanguageName)
                    .Parameters(p => p.TopN(3));

                var cafes = (await executor.GetMappedResult<Cafe>(builder, cancellationToken: cancellationToken)).ToArray();

                var cacheDependencyKeys = cafes.Select(c => "contentitem|byid|" + c.SystemFields.ContentItemID).ToList();
                cacheDependencyKeys.Add($"webpage|byid|{webPage.WebPageItemID}|{webPage.LanguageName}");
                cacheDependencyKeys.Add($"{SmartFolderInfo.OBJECT_TYPE}|byguid|{homePage.HomePageCafesFolder.Identifier}");
                settings.CacheDependency = CacheHelper.GetCacheDependency(cacheDependencyKeys);

                return cafes;
            }, new CacheSettings(5, DancingGoatConstants.WEBSITE_CHANNEL_NAME, nameof(Cafe), webPage.WebPageItemID, webPage.LanguageName), HttpContext.RequestAborted);

            return View(HomePageViewModel.GetViewModel(homePage, cafes));
        }
    }
}
