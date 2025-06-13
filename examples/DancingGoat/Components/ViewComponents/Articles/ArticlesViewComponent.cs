using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using CMS.Websites;

using DancingGoat.Models;

using Kentico.Content.Web.Mvc;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;

namespace DancingGoat.ViewComponents
{
    /// <summary>
    /// Controller for article view component.
    /// </summary>
    public class ArticlesViewComponent : ViewComponent
    {
        private readonly IContentRetriever contentRetriever;

        private const int ARTICLES_PER_VIEW = 5;


        public ArticlesViewComponent(
            IContentRetriever contentRetriever)
        {
            this.contentRetriever = contentRetriever;
        }


        public async Task<ViewViewComponentResult> InvokeAsync(WebPageRelatedItem articlesSectionItem)
        {
            var articlesSection = (await contentRetriever.RetrievePagesByGuids<ArticlesSection>(
                [articlesSectionItem.WebPageGuid],
                HttpContext.RequestAborted
            )).FirstOrDefault();

            if (articlesSection == null)
            {
                return View("~/Components/ViewComponents/Articles/Default.cshtml", ArticlesSectionViewModel.GetViewModel(null, Enumerable.Empty<ArticleViewModel>(), string.Empty));
            }

            IEnumerable<ArticlePage> articlePages = await GetArticlePages(articlesSection);

            var models = new List<ArticleViewModel>();
            foreach (var article in articlePages)
            {
                var model = ArticleViewModel.GetViewModel(article);
                models.Add(model);
            }

            var viewModel = ArticlesSectionViewModel.GetViewModel(articlesSection, models, articlesSection.GetUrl().RelativePath);

            return View("~/Components/ViewComponents/Articles/Default.cshtml", viewModel);
        }

        private async Task<IEnumerable<ArticlePage>> GetArticlePages(ArticlesSection articlesSection)
        {
            return await contentRetriever.RetrievePages<ArticlePage>(
                new RetrievePagesParameters
                {
                    LinkedItemsMaxLevel = 1,
                    PathMatch = PathMatch.Children(articlesSection.SystemFields.WebPageItemTreePath)
                },
                query => query.TopN(ARTICLES_PER_VIEW),
                new RetrievalCacheSettings($"TopN_{ARTICLES_PER_VIEW}"),
                HttpContext.RequestAborted
            );
        }
    }
}
