using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using CMS.Websites;

namespace DancingGoat.Models
{
    public record ArticleDetailViewModel(string Title, string TeaserUrl, string Summary, string Text, DateTime PublicationDate, Guid Guid, bool IsSecured, string Url, IEnumerable<RelatedArticleViewModel> RelatedArticles)
        : IWebPageBasedViewModel
    {
        /// <inheritdoc/>
        public IWebPageFieldsSource WebPage { get; init; }


        /// <summary>
        /// Validates and maps <see cref="ArticlePage"/> to a <see cref="ArticleDetailViewModel"/>.
        /// </summary>
        public static async Task<ArticleDetailViewModel> GetViewModel(ArticlePage articlePage, string languageName, ArticlePageRepository articlePageRepository, IWebPageUrlRetriever urlRetriever)
        {
            var teaser = articlePage.ArticlePageTeaser.FirstOrDefault();

            var relatedArticles = await articlePageRepository
                .GetArticles(articlePage.ArticleRelatedArticles.Select(article => article.WebPageGuid).ToList(), languageName);

            var relatedArticlesViewModels = new List<RelatedArticleViewModel>();

            foreach (var relatedArticle in relatedArticles)
            {
                relatedArticlesViewModels.Add(await RelatedArticleViewModel.GetViewModel(relatedArticle, urlRetriever, languageName));
            }

            var url = await urlRetriever.Retrieve(articlePage, languageName);

            return new ArticleDetailViewModel(
                articlePage.ArticleTitle,
                teaser?.ImageFile.Url,
                articlePage.ArticlePageSummary,
                articlePage.ArticlePageText,
                articlePage.ArticlePagePublishDate,
                articlePage.SystemFields.ContentItemGUID,
                articlePage.SystemFields.ContentItemIsSecured,
                url.RelativePath,
                relatedArticlesViewModels)
            {
                WebPage = articlePage
            };
        }
    }
}
