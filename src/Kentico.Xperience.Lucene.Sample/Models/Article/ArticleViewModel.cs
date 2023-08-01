using System;
using System.Linq;

using CMS.DocumentEngine.Types.DancingGoatCore;

using Kentico.Content.Web.Mvc;

namespace DancingGoat.Models
{
    public class ArticleViewModel
    {
        public string TeaserPath{ get; set; }


        public string TeaserShortDescription { get; set; }


        public string Title { get; set; }


        public DateTime PublicationDate { get; set; }


        public string Summary { get; set; }


        public string Text { get; set; }


        public string Url { get; set; }


        public bool IsSecured { get; set; }


        public static ArticleViewModel GetViewModel(Article article, IPageUrlRetriever pageUrlRetriever)
        {
            var articleMedia = article?.Fields.Teaser.FirstOrDefault() as Media;

            return new ArticleViewModel
            {
                PublicationDate = article.PublicationDate,
                Summary = article.Fields.Summary,
                TeaserPath = articleMedia?.Fields.File.Url,
                TeaserShortDescription = articleMedia?.Fields.ShortDescription ?? string.Empty,
                Text = article.Fields.Text,
                Title = article.DocumentName,
                Url = pageUrlRetriever.Retrieve(article).RelativePath,
                IsSecured = article.NodeIsSecured
            };
        }
    }
}