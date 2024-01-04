using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using DancingGoat.Models;
using DancingGoat.Widgets;

using Kentico.Content.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;

[assembly: RegisterWidget(ArticlesWidgetViewComponent.IDENTIFIER, typeof(ArticlesWidgetViewComponent), "Latest articles", typeof(ArticlesWidgetProperties), Description = "Displays the latest articles from the Dancing Goat sample site.", IconClass = "icon-l-list-article", AllowCache = true)]

namespace DancingGoat.Widgets
{
    /// <summary>
    /// Controller for article widget.
    /// </summary>
    public class ArticlesWidgetViewComponent : ViewComponent
    {
        /// <summary>
        /// Widget identifier.
        /// </summary>
        public const string IDENTIFIER = "DancingGoat.HomePage.ArticlesWidget";

        private static string[] dependency_keys = new[] { "node|dancinggoatcore|/articles|childnodes" };

        private readonly ArticleRepository repository;
        private readonly IPageUrlRetriever pageUrlRetriever;


        /// <summary>
        /// Creates an instance of <see cref="ArticlesWidgetViewComponent"/> class.
        /// /// </summary>
        /// <param name="repository">Article repository.</param>
        /// <param name="pageUrlRetriever">Retriever for page URLs.</param>
        /// <param name="mediaFileUrlHelper">Media file URL helper.</param>
        public ArticlesWidgetViewComponent(ArticleRepository repository, IPageUrlRetriever pageUrlRetriever)
        {
            this.repository = repository;
            this.pageUrlRetriever = pageUrlRetriever;
        }


        /// <summary>
        /// Returns the model used by widgets' view.
        /// </summary>
        /// <param name="properties">Widget properties.</param>
        public async Task<ViewViewComponentResult> InvokeAsync(ComponentViewModel<ArticlesWidgetProperties> viewModel)
        {
            if (viewModel is null)
            {
                throw new ArgumentNullException(nameof(viewModel));
            }

            viewModel.CacheDependencies.CacheKeys = dependency_keys;

            var articles = await repository.GetArticles(ContentItemIdentifiers.ARTICLES, viewModel.Properties.Count);

            var articlesModel = new List<ArticleViewModel>();

            foreach (var article in articles)
            {
                articlesModel.Add(ArticleViewModel.GetViewModel(article, pageUrlRetriever));
            }

            return View("~/Components/Widgets/Articles/_ArticlesWidget.cshtml", new ArticlesWidgetViewModel { Articles = articlesModel, Count = viewModel.Properties.Count });
        }
    }
}
