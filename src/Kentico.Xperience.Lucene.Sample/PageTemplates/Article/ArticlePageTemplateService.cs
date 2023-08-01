using CMS.DocumentEngine.Types.DancingGoatCore;

using Kentico.Content.Web.Mvc;

namespace DancingGoat.PageTemplates
{
    public class ArticlePageTemplateService
    {
        private readonly IPageDataContextRetriever pageDataContextRetriver;


        public ArticlePageTemplateService(IPageDataContextRetriever pageDataContextRetriver)
        {
            this.pageDataContextRetriver = pageDataContextRetriver;
        }


        public ArticleViewModel GetTemplateModel()
        {
            var article = pageDataContextRetriver.Retrieve<Article>().Page;
            return ArticleViewModel.GetViewModel(article);
        }
    }
}