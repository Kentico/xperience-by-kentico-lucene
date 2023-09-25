using System.Collections.Generic;
using System.Threading.Tasks;
using CMS.ContentEngine;
using CMS.Websites;
using DancingGoat.Controllers;
using DancingGoat.Helpers;
using DancingGoat.Models;

using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;

using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWebPageRoute(ArticlesSection.CONTENT_TYPE_NAME, typeof(DancingGoatArticleController), WebsiteChannelNames = new[] { WebsiteConstants.WEBSITE_CHANNEL_NAME })]
[assembly: RegisterWebPageRoute(ArticlePage.CONTENT_TYPE_NAME, typeof(DancingGoatArticleController), WebsiteChannelNames = new[] { WebsiteConstants.WEBSITE_CHANNEL_NAME }, ActionName = "Article")]

namespace DancingGoat.Controllers
{
    public class DancingGoatArticleController : Controller
    {
        private readonly ArticlePageRepository articlePageRepository;
        private readonly IWebPageUrlRetriever urlRetriever;
        private readonly IWebPageDataContextRetriever webPageDataContextRetriever;
        private readonly ICurrentLanguageRetriever currentLanguageRetriever;
        private readonly IContentQueryExecutor executor;
        private readonly IWebPageQueryResultMapper mapper;

        public DancingGoatArticleController(ArticlePageRepository articlePageRepository, IWebPageUrlRetriever urlRetriever, IWebPageDataContextRetriever webPageDataContextRetriever, ICurrentLanguageRetriever currentLanguageRetriever, IContentQueryExecutor executor, IWebPageQueryResultMapper mapper)
        {
            this.articlePageRepository = articlePageRepository;
            this.urlRetriever = urlRetriever;
            this.webPageDataContextRetriever = webPageDataContextRetriever;
            this.currentLanguageRetriever = currentLanguageRetriever;
            this.executor = executor;
            this.mapper = mapper;
        }


        public async Task<IActionResult> Index()
        {
            var languageName = currentLanguageRetriever.Get();





            //Use this
            var queryBuilder = new ContentItemQueryBuilder()
                    .ForContentType(ArticlePage.CONTENT_TYPE_NAME, config => config.WithLinkedItems(1).ForWebsite(WebsiteConstants.WEBSITE_CHANNEL_NAME, includeUrlPath: true))
                    .InLanguage(languageName);

            var nodes = await executor.GetWebPageResult(queryBuilder, container => mapper.Map<ArticlePage>(container));



            var articles = await articlePageRepository.GetArticles(languageName, true, cancellationToken: HttpContext.RequestAborted);

            var models = new List<ArticleViewModel>();
            foreach (var article in articles)
            {
                var model = await ArticleViewModel.GetViewModel(article, urlRetriever, languageName);
                models.Add(model);
            }

            return View(models);
        }


        public async Task<IActionResult> Article()
        {
            var languageName = currentLanguageRetriever.Get();
            var webPageItemId = webPageDataContextRetriever.Retrieve().WebPage.WebPageItemID;

            var article = await articlePageRepository.GetArticle(webPageItemId, languageName, HttpContext.RequestAborted);

            if (article is null)
            {
                return NotFound();
            }

            return View(await ArticleDetailViewModel.GetViewModel(article, languageName, articlePageRepository, urlRetriever));
        }
    }
}
