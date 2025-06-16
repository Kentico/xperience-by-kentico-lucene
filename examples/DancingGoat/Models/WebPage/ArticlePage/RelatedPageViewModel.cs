using System;
using System.Linq;
using System.Net;

using CMS.Websites;

namespace DancingGoat.Models
{
    public record RelatedPageViewModel(string Title, string TeaserUrl, string Summary, DateTime? PublicationDate, string Url)
    {
        /// <summary>
        /// Validates and maps <see cref="ArticlePage"/> or <see cref="ProductPage"/> to a <see cref="RelatedPageViewModel"/>.
        /// </summary>
        public static RelatedPageViewModel GetViewModel(IWebPageFieldsSource webPage)
        {
            if (webPage is ArticlePage article)
            {
                return GetViewModelFromArticlePage(article);
            }
            else if (webPage is ProductPage productPage)
            {
                return GetViewModelFromProductPage(productPage);
            }

            throw new ArgumentException($"Param {nameof(webPage)} must be {nameof(ArticlePage)} or {nameof(ProductPage)}");
        }


        private static RelatedPageViewModel GetViewModelFromArticlePage(ArticlePage articlePage)
        {
            return new RelatedPageViewModel
            (
                articlePage.ArticleTitle,
                articlePage.ArticlePageTeaser.FirstOrDefault()?.ImageFile.Url,
                WebUtility.HtmlEncode(articlePage.ArticlePageSummary),
                articlePage.ArticlePagePublishDate,
                articlePage.GetUrl().RelativePath
            );
        }


        private static RelatedPageViewModel GetViewModelFromProductPage(ProductPage productPage)
        {
            var product = productPage.ProductPageProduct.FirstOrDefault() as IProductFields;

            return new RelatedPageViewModel
            (
                product?.ProductFieldName,
                product?.ProductFieldImage.FirstOrDefault()?.ImageFile.Url ?? string.Empty,
                product?.ProductFieldDescription,
                null,
                productPage.GetUrl().RelativePath
            );
        }
    }
}
