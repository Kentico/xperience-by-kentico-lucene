using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using CMS.DataEngine;
using CMS.Websites;

using DancingGoat;
using DancingGoat.Controllers;
using DancingGoat.Models;

using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;

using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWebPageRoute(ArticlesSection.CONTENT_TYPE_NAME, typeof(DancingGoatArticleController), WebsiteChannelNames = new[] { DancingGoatConstants.WEBSITE_CHANNEL_NAME })]
[assembly: RegisterWebPageRoute(ArticlePage.CONTENT_TYPE_NAME, typeof(DancingGoatArticleController), WebsiteChannelNames = new[] { DancingGoatConstants.WEBSITE_CHANNEL_NAME }, ActionName = "Article")]

namespace DancingGoat.Controllers
{
    public class DancingGoatArticleController : Controller
    {
        private readonly IContentRetriever contentRetriever;


        public DancingGoatArticleController(IContentRetriever contentRetriever)
        {
            this.contentRetriever = contentRetriever;
        }


        public async Task<IActionResult> Index()
        {
            var articlesSection = await contentRetriever.RetrieveCurrentPage<ArticlesSection>(
                HttpContext.RequestAborted
            );

            var articles = await GetArticles(articlesSection);

            var models = articles.Select(ArticleViewModel.GetViewModel);

            var model = ArticlesSectionViewModel.GetViewModel(articlesSection, models, articlesSection.GetUrl().RelativePath);

            return View(model);
        }


        public async Task<IActionResult> Article()
        {
            var article = await contentRetriever.RetrieveCurrentPage<ArticlePage>(
                new RetrieveCurrentPageParameters { IncludeSecuredItems = true, LinkedItemsMaxLevel = 3 },
                HttpContext.RequestAborted
            );

            if (article is null)
            {
                return NotFound();
            }

            var model = ArticleDetailViewModel.GetViewModel(article);

            return new TemplateResult(model);
        }


        private async Task<IEnumerable<ArticlePage>> GetArticles(ArticlesSection articlesSection)
        {
            return await contentRetriever.RetrievePages<ArticlePage>(
                new RetrievePagesParameters
                {
                    PathMatch = PathMatch.Children(articlesSection.SystemFields.WebPageItemTreePath),
                    IncludeSecuredItems = true,
                    LinkedItemsMaxLevel = 1
                },
                query => query.OrderBy(OrderByColumn.Desc(nameof(ArticlePage.ArticlePagePublishDate))),
                new RetrievalCacheSettings($"OrderBy_{nameof(ArticlePage.ArticlePagePublishDate)}_{nameof(OrderByColumn.Desc)}"),
                HttpContext.RequestAborted
            );
        }
    }
}
