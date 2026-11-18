using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Helpers;

using DancingGoat.Models;
using DancingGoat.Services;
using DancingGoat.Widgets;

using Kentico.Content.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;

[assembly: RegisterWidget(CafeCardsWidgetViewComponent.IDENTIFIER, typeof(CafeCardsWidgetViewComponent), "{$dancinggoat.cafecardswidget.title$}", typeof(CafeCardsWidgetProperties), Description = "{$dancinggoat.cafecardswidget.description$}", IconClass = "icon-cup")]

namespace DancingGoat.Widgets
{
    /// <summary>
    /// View component for Cafe cards widget.
    /// </summary>
    public class CafeCardsWidgetViewComponent : ViewComponent
    {
        /// <summary>
        /// Widget identifier.
        /// </summary>
        public const string IDENTIFIER = "DancingGoat.General.CafeCardsWidget";


        private readonly IContentRetriever contentRetriever;
        private readonly ICacheDependencyBuilderFactory cacheDependencyBuilderFactory;
        private readonly WebPageUrlProvider webPageUrlProvider;


        /// <summary>
        /// Creates an instance of <see cref="CafeCardsWidgetViewComponent"/> class.
        /// </summary>
        /// <param name="contentRetriever">Content retriever.</param>
        /// <param name="cacheDependencyBuilderFactory">Cache dependency builder factory.</param>
        /// <param name="webPageUrlProvider">Web page URL provider.</param>
        public CafeCardsWidgetViewComponent(IContentRetriever contentRetriever, ICacheDependencyBuilderFactory cacheDependencyBuilderFactory, WebPageUrlProvider webPageUrlProvider)
        {
            this.contentRetriever = contentRetriever;
            this.cacheDependencyBuilderFactory = cacheDependencyBuilderFactory;
            this.webPageUrlProvider = webPageUrlProvider;
        }


        public async Task<ViewViewComponentResult> InvokeAsync(CafeCardsWidgetProperties properties, CancellationToken cancellationToken)
        {
            var cafes = await GetCafes(properties, cancellationToken);
            string contactsPagePath = await GetContactsPagePath(cancellationToken);

            var model = new CafeCardsWidgetViewModel
            {
                Heading = properties.Heading,
                Cafes = cafes.Select(CafeViewModel.GetViewModel).ToList(),
                ContactsPagePath = contactsPagePath
            };

            return View("~/Components/Widgets/CafeCardsWidget/_CafeCardsWidget.cshtml", model);
        }


        private async Task<IEnumerable<Cafe>> GetCafes(CafeCardsWidgetProperties properties, CancellationToken cancellationToken)
        {
            var folderIdentifier = properties.CafesFolder?.Identifier ?? Guid.Empty;
            if (folderIdentifier == Guid.Empty)
            {
                return Enumerable.Empty<Cafe>();
            }

            var cafeAdditionalDependencies = cacheDependencyBuilderFactory.Create()
                .ForInfoObjects<SmartFolderInfo>()
                    .ByGuid(folderIdentifier)
                    .Builder()
                .Build();

            return await contentRetriever.RetrieveContent<Cafe>(
                new RetrieveContentParameters { LinkedItemsMaxLevel = 1 },
                query => query
                    .InSmartFolder(folderIdentifier)
                    .OrderBy(nameof(Cafe.CafeOrder))
                    .TopN(properties.CafesCount),
                new RetrievalCacheSettings($"InSmartFolder_{folderIdentifier}_OrderByCafeOrder_TopN_{properties.CafesCount}", TimeSpan.FromMinutes(5), additionalCacheDependencies: cafeAdditionalDependencies),
                cancellationToken
            );
        }


        private async Task<string> GetContactsPagePath(CancellationToken cancellationToken)
        {
            return await webPageUrlProvider.ContactsPageUrl(cancellationToken: cancellationToken);
        }
    }
}
