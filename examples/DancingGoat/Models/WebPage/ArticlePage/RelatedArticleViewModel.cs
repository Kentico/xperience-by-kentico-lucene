using System;
using System.Linq;
using System.Threading.Tasks;

using CMS.Websites;

namespace DancingGoat.Models
{
    public record RelatedArticleViewModel(string Title, string TeaserUrl, string Summary, string Text, DateTime PublicationDate, Guid Guid, string Url)
    {
        /// <summary>
        /// Validates and maps <see cref="ArticlePage"/> to a <see cref="RelatedArticleViewModel"/>.
        /// </summary>
        public static async Task<RelatedArticleViewModel> GetViewModel(ArticlePage articlePage, IWebPageUrlRetriever urlRetriever, string languageName)
        {
            var url = await urlRetriever.Retrieve(articlePage, languageName);

            return new RelatedArticleViewModel
            (
                articlePage.ArticleTitle,
                articlePage.ArticlePageTeaser.FirstOrDefault()?.ImageFile.Url,
                articlePage.ArticlePageSummary,
                articlePage.ArticlePageText,
                articlePage.ArticlePagePublishDate,
                articlePage.SystemFields.ContentItemGUID,
                url.RelativePath
            );
        }
    }
}
