using System.Collections.Generic;

namespace DancingGoat.Models
{
    public record ArticlesSectionViewModel(IEnumerable<ArticleViewModel> Articles, string ArticlesPath)
    {
        public static ArticlesSectionViewModel GetViewModel(IEnumerable<ArticleViewModel> Articles, string ArticlesPath)
        {
            return new ArticlesSectionViewModel(Articles, ArticlesPath);
        }
    }
}
