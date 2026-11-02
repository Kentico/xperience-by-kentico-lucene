using System;
using System.Collections.Generic;
using System.Linq;

using CMS.ContentEngine;
using CMS.Websites;

namespace DancingGoat.Models
{
    public record ArticleDetailViewModel(string Title, string TeaserUrl, string Summary, string Text, DateTime PublicationDate, Guid Guid, bool IsSecured, string Url, IEnumerable<RelatedPageViewModel> RelatedPages)
        : IWebPageBasedViewModel
    {
        /// <inheritdoc/>
        public IWebPageFieldsSource WebPage { get; init; }


        /// <summary>
        /// Gets the AI provenance origin of the given field of the article page.
        /// </summary>
        /// <param name="fieldName">Code name of the article page field.</param>
        public AiProvenanceOrigin GetAiProvenanceOrigin(string fieldName) => WebPage?.GetAiProvenanceOrigin(fieldName) ?? AiProvenanceOrigin.None;


        /// <summary>
        /// Validates and maps <see cref="ArticlePage"/> to a <see cref="ArticleDetailViewModel"/>.
        /// </summary>
        public static ArticleDetailViewModel GetViewModel(ArticlePage articlePage)
        {
            var teaser = articlePage.ArticlePageTeaser.FirstOrDefault();

            var relatedPageViewModels = new List<RelatedPageViewModel>();

            foreach (var relatedPage in articlePage.ArticleRelatedPages)
            {
                relatedPageViewModels.Add(RelatedPageViewModel.GetViewModel(relatedPage));
            }

            return new ArticleDetailViewModel(
                articlePage.ArticleTitle,
                teaser?.ImageFile.Url,
                articlePage.ArticlePageSummary,
                articlePage.ArticlePageText,
                articlePage.ArticlePagePublishDate,
                articlePage.SystemFields.ContentItemGUID,
                articlePage.SystemFields.ContentItemIsSecured,
                articlePage.GetUrl().RelativePath,
                relatedPageViewModels)
            {
                WebPage = articlePage
            };
        }
    }
}
