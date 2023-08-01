using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using CMS.DocumentEngine;
using CMS.DocumentEngine.Types.DancingGoatCore;

using DancingGoat.Infrastructure;

using Kentico.Content.Web.Mvc;

namespace DancingGoat.Models
{
    /// <summary>
    /// A service for retrieving the navigation menu items.
    /// </summary>
    public class NavigationService
    {
        private const string MENU_PATH = "/Navigation-menu";

        private readonly IPageRetriever pageRetriever;
        private readonly IPageUrlRetriever pageUrlRetriever;
        private readonly RepositoryCacheHelper repositoryCacheHelper;


        /// <summary>
        /// Initializes a new instance of the <see cref="NavigationService"/> class.
        /// </summary>
        /// <param name="pageRetriever">The page retriever.</param>
        /// <param name="pageUrlRetriever">The page URL retriever.</param>
        /// <param name="repositoryCacheHelper">Handles caching of retrieved objects.</param>
        public NavigationService(IPageRetriever pageRetriever, IPageUrlRetriever pageUrlRetriever, RepositoryCacheHelper repositoryCacheHelper)
        {
            this.pageRetriever = pageRetriever;
            this.repositoryCacheHelper = repositoryCacheHelper;
            this.pageUrlRetriever = pageUrlRetriever;
        }


        /// <summary>
        /// Returns an enumerable collection of navigation items ordered by the content tree order and level.
        /// </summary>
        public async Task<IEnumerable<NavigationItemViewModel>> GetNavigationItems()
        {
            var navigationItems = await pageRetriever.RetrieveAsync<NavigationItem>(
                query => query
                    .Path(MENU_PATH, PathTypeEnum.Children)
                    .OrderByAscending(nameof(TreeNode.NodeOrder)),
                cache => cache
                    .Key($"{nameof(NavigationService)}|{nameof(GetNavigationItems)}|navigationitems|{MENU_PATH}")
                    // Flushes the cache when a new navigation item is created or page order changed
                    .Dependencies((pages, builder) => builder.PagePath(MENU_PATH, PathTypeEnum.Children).PageOrder())
                    .Expiration(TimeSpan.FromDays(1), useSliding: true));

            var linkToPages = await pageRetriever.RetrieveAsync<TreeNode>(
                query => query
                    .WhereIn(nameof(TreeNode.NodeGUID), navigationItems.Select(i => i.Fields.LinkTo.First().NodeGuid).ToList())
                    .GetEnumerableTypedResult(),
                cache => cache
                    .Key($"{nameof(NavigationService)}|{nameof(GetNavigationItems)}|treenodes|{MENU_PATH}")
                    // Flushes the cache when the page data changes (could contain a URL slug update)
                    .Dependencies((pages, builder) => builder.Pages(pages).PagePath(MENU_PATH))
                    .Expiration(TimeSpan.FromDays(1), useSliding: true)
            );

            var navigationItemViewModels = navigationItems.Join(
                linkToPages,
                navigationItem => navigationItem.Fields.LinkTo.First().NodeGuid,
                treeNode => treeNode.NodeGUID,
                (navigationItem, treeNode) => new NavigationItemViewModel
                {
                    Caption = navigationItem.DocumentName,
                    RelativePath = pageUrlRetriever.Retrieve(treeNode).RelativePath
                });
            return navigationItemViewModels;
        }
    }
}