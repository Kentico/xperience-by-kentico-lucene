using System.Collections.Generic;
using System.Threading.Tasks;

using DancingGoat.Models;

using CMS.Websites.Routing;
using Kentico.Content.Web.Mvc;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using CMS.Websites;

namespace DancingGoat.ViewComponents
{
    /// <summary>
    /// Controller for article view component.
    /// </summary>
    public class ArticlesViewComponent : ViewComponent
    {
        private readonly ArticlePageRepository articlePageRepository;
        private readonly IWebPageUrlRetriever urlRetriever;
        private readonly ICurrentLanguageRetriever currentLanguageRetriever;
        private readonly IWebsiteChannelContext websiteChannelContext;

        private const int ARTICLES_PER_VIEW = 5;


        public ArticlesViewComponent(ArticlePageRepository articlePageRepository, IWebPageUrlRetriever urlRetriever, ICurrentLanguageRetriever currentLanguageRetriever, IWebsiteChannelContext websiteChannelContext)
        {
            this.articlePageRepository = articlePageRepository;
            this.urlRetriever = urlRetriever;
            this.currentLanguageRetriever = currentLanguageRetriever;
            this.websiteChannelContext = websiteChannelContext;
        }


        public async Task<ViewViewComponentResult> InvokeAsync()
        {
            var languageName = currentLanguageRetriever.Get();

            var articlePages = await articlePageRepository.GetArticles(languageName, false, ARTICLES_PER_VIEW, HttpContext.RequestAborted);

            var models = new List<ArticleViewModel>();
            foreach (var article in articlePages)
            {
                var model = await ArticleViewModel.GetViewModel(article, urlRetriever, languageName);
                models.Add(model);
            }
            var url = (await urlRetriever.Retrieve("/Articles", websiteChannelContext.WebsiteChannelName, languageName)).RelativePath;

            var viewModel = ArticlesSectionViewModel.GetViewModel(models, url);

            return View("~/Components/ViewComponents/Articles/Default.cshtml", viewModel);
        }
    }
}
