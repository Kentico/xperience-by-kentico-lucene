using System;
using System.Linq;

using CMS.Websites;

namespace DancingGoat.Models
{
    public record ArticleViewModel(string Title, string TeaserUrl, string Summary, string Text, DateTime PublicationDate, Guid Guid, bool IsSecured, string Url)
    {
        /// <summary>
        /// Validates and maps <see cref="ArticlePage"/> to a <see cref="ArticleViewModel"/>.
        /// </summary>
        public static ArticleViewModel GetViewModel(ArticlePage articlePage)
        {
            var teaser = articlePage.ArticlePageTeaser.FirstOrDefault();

            var url = articlePage.GetUrl();

            return new ArticleViewModel(
                articlePage.ArticleTitle,
                teaser?.ImageFile.Url,
                articlePage.ArticlePageSummary,
                articlePage.ArticlePageText,
                articlePage.ArticlePagePublishDate,
                articlePage.SystemFields.ContentItemGUID,
                articlePage.SystemFields.ContentItemIsSecured,
                url.RelativePath
            );
        }
    }
}
