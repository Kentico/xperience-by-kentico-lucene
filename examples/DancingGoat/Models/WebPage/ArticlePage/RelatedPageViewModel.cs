using System;
using System.Linq;
using System.Threading.Tasks;

using CMS.Websites;

namespace DancingGoat.Models
{
    public record RelatedPageViewModel(string Title, string TeaserUrl, string Summary, DateTime? PublicationDate, string Url)
    {
        /// <summary>
        /// Validates and maps <see cref="ArticlePage"/> or <see cref="CoffeePage"/> to a <see cref="RelatedPageViewModel"/>.
        /// </summary>
        public static Task<RelatedPageViewModel> GetViewModel(IWebPageFieldsSource webPage, IWebPageUrlRetriever urlRetriever, string languageName)
        {
            if (webPage is ArticlePage article)
            {
                return GetViewModelFromArticlePage(article, urlRetriever, languageName);
            }
            else if (webPage is CoffeePage coffee)
            {
                return GetViewModelFromCoffeePage(coffee, urlRetriever, languageName);
            }

            throw new ArgumentException($"Param {nameof(webPage)} must be {nameof(ArticlePage)} or {nameof(CoffeePage)}");
        }


        private static async Task<RelatedPageViewModel> GetViewModelFromArticlePage(ArticlePage articlePage, IWebPageUrlRetriever urlRetriever, string languageName)
        {
            var url = await urlRetriever.Retrieve(articlePage, languageName);

            return new RelatedPageViewModel
            (
                articlePage.ArticleTitle,
                articlePage.ArticlePageTeaser.FirstOrDefault()?.ImageFile.Url,
                articlePage.ArticlePageSummary,
                articlePage.ArticlePagePublishDate,
                url.RelativePath
            );
        }


        private static async Task<RelatedPageViewModel> GetViewModelFromCoffeePage(CoffeePage coffeePage, IWebPageUrlRetriever urlRetriever, string languageName)
        {
            var url = await urlRetriever.Retrieve(coffeePage, languageName);

            return new RelatedPageViewModel
            (
                coffeePage.RelatedItem.FirstOrDefault()?.ProductFieldsName,
                coffeePage.RelatedItem.FirstOrDefault()?.ProductFieldsImage.FirstOrDefault()?.ImageFile.Url ?? string.Empty,
                coffeePage.RelatedItem.FirstOrDefault()?.ProductFieldsShortDescription,
                null,
                url.RelativePath
            );
        }

    }
}
