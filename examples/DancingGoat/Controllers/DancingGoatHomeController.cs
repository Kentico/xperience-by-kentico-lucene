using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Helpers;
using CMS.Websites;

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
        private readonly IContentRetriever contentRetriever;
        private readonly ICacheDependencyBuilderFactory cacheDependencyBuilderFactory;

        public DancingGoatHomeController(IContentRetriever contentRetriever, ICacheDependencyBuilderFactory cacheDependencyBuilderFactory)
        {
            this.contentRetriever = contentRetriever;
            this.cacheDependencyBuilderFactory = cacheDependencyBuilderFactory;
        }

        public async Task<IActionResult> Index()
        {
            var homePage = await contentRetriever.RetrieveCurrentPage<HomePage>(
                new RetrieveCurrentPageParameters { LinkedItemsMaxLevel = 4 },
                HttpContext.RequestAborted
            );

            var cafes = await GetCafes(homePage);

            return View(HomePageViewModel.GetViewModel(homePage, cafes));
        }

        private async Task<IEnumerable<Cafe>> GetCafes(HomePage homePage)
        {
            var cafeAdditionalDependencies = cacheDependencyBuilderFactory.Create()
                .ForWebPageItems()
                    .ByIdWithLanguageContext(homePage.SystemFields.WebPageItemID)
                    .Builder()
                .ForInfoObjects<SmartFolderInfo>()
                    .ByGuid(homePage.HomePageCafesFolder.Identifier)
                    .Builder()
                .Build();

            return await contentRetriever.RetrieveContent<Cafe>(
                new RetrieveContentParameters { LinkedItemsMaxLevel = 1 },
                query => query
                    .InSmartFolder(homePage.HomePageCafesFolder.Identifier)
                    .TopN(3),
                new RetrievalCacheSettings($"InSmartFolder_{homePage.HomePageCafesFolder.Identifier}_TopN_3", TimeSpan.FromMinutes(5), additionalCacheDependencies: cafeAdditionalDependencies),
                HttpContext.RequestAborted
            );
        }
    }
}
