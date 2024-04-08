using System.Collections.Generic;

using CMS.Websites;

namespace DancingGoat.Models
{
    public record ArticlesSectionViewModel(IEnumerable<ArticleViewModel> Articles, string ArticlesPath)
        : IWebPageBasedViewModel
    {
        /// <inheritdoc/>
        public IWebPageFieldsSource WebPage { get; init; }


        /// <summary>
        /// Maps <see cref="ArticlesSection"/> to a <see cref="ArticlesSectionViewModel"/>.
        /// </summary>
        public static ArticlesSectionViewModel GetViewModel(ArticlesSection articlesSection, IEnumerable<ArticleViewModel> Articles, string ArticlesPath)
        {
            return new ArticlesSectionViewModel(Articles, ArticlesPath)
            {
                WebPage = articlesSection
            };
        }
    }
}
