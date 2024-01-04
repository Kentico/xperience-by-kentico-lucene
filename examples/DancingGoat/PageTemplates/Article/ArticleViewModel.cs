using System;
using System.Linq;

using CMS.DocumentEngine.Types.DancingGoatCore;

namespace DancingGoat.PageTemplates
{
    public class ArticleViewModel
    {
        public string TeaserPath { get; set; }


        public string Title { get; set; }


        public DateTime PublicationDate { get; set; }


        public string Text { get; set; }


        public static ArticleViewModel GetViewModel(Article article)
        {
            return new ArticleViewModel
            {
                PublicationDate = article.PublicationDate,
                TeaserPath = (article.Fields.Teaser.FirstOrDefault() as Media)?.Fields.File?.Url,
                Text = article.Fields.Text,
                Title = article.DocumentName
            };
        }
    }
}