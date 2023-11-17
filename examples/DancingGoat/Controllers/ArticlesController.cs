using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using CMS.DocumentEngine;
using CMS.DocumentEngine.Types.DancingGoatCore;

using DancingGoat.Controllers;
using DancingGoat.Models;

using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;

using Microsoft.AspNetCore.Mvc;

[assembly: RegisterPageRoute(ArticleSection.CLASS_NAME, typeof(ArticlesController))]
[assembly: RegisterPageRoute(Article.CLASS_NAME, typeof(ArticlesController), ActionName = "Detail")]

namespace DancingGoat.Controllers
{
    public class ArticlesController : Controller
    {
        private readonly ArticleRepository articleRepository;

        public ArticlesController(ArticleRepository articleRepository)
        {
            this.articleRepository = articleRepository;
        }


        public async Task<IActionResult> Index([FromServices] IPageDataContextRetriever dataContextRetriever,
                                   [FromServices] IPageUrlRetriever pageUrlRetriever,
                                   CancellationToken cancellationToken)
        {
            var section = dataContextRetriever.Retrieve<TreeNode>().Page;
            var articles = await articleRepository.GetArticles(section.NodeAliasPath, cancellationToken: cancellationToken);

            var articlesModel = new List<ArticleViewModel>();

            foreach (var article in articles)
            {
                articlesModel.Add(ArticleViewModel.GetViewModel(article, pageUrlRetriever));
            }

            return View(articlesModel);
        }


        public async Task<IActionResult> Detail([FromServices] ArticleRepository articleRepository, CancellationToken cancellationToken)
        {
            var article = await articleRepository.GetCurrent(2, cancellationToken);

            return new TemplateResult(article);
        }
    }
}