using Kentico.Content.Web.Mvc.PageBuilder;

namespace DancingGoat.Models
{
    /// <summary>
    /// Helper class for retrieving values from web page based view models which include reusable schema fields "SEO Fields".
    /// </summary>
    public static class SEOSchemaModelDataRetriever
    {
        /// <summary>
        /// Returns value of title field from the given model.
        /// </summary>
        /// <remarks>
        /// View model must satisfy following requirements:<br/>
        /// - is a page builder page based on <see cref="TemplateViewModel"/> class<br/>
        /// - implements <see cref="ISEOFields"/><br/>
        /// - implements <see cref="IWebPageBasedViewModel"/> and its base page <see cref="IWebPageBasedViewModel.WebPage"/> implements <see cref="ISEOFields"/><br/>
        /// <para>In other cases the method returns null.</para>
        /// </remarks>
        /// <param name="model">View model.</param>
        public static string GetTitleValue(object model)
        {
            string title = null;

            var viewModel = model;
            if (viewModel is TemplateViewModel templateViewModel)
            {
                viewModel = templateViewModel.GetTemplateModel<object>();
            }

            if (viewModel is ISEOFields webPageWithSEO)
            {
                title = webPageWithSEO.SEOFieldsTitle;
            }
            else if (viewModel is IWebPageBasedViewModel webPageBasedViewModel)
            {
                if (webPageBasedViewModel.WebPage is ISEOFields basePageWithSEO)
                {
                    title = basePageWithSEO.SEOFieldsTitle;
                }
            }

            return title;
        }


        /// <summary>
        /// Returns value of description field from the given model.
        /// </summary>
        /// <remarks>
        /// View model must satisfy following requirements:<br/>
        /// - is a page builder page based on <see cref="TemplateViewModel"/> class<br/>
        /// - implements <see cref="ISEOFields"/><br/>
        /// - implements <see cref="IWebPageBasedViewModel"/> and its base page <see cref="IWebPageBasedViewModel.WebPage"/> implements <see cref="ISEOFields"/><br/>
        /// <para>In other cases the method returns null.</para>
        /// </remarks>
        /// <param name="model">Model</param>
        public static string GetDescriptionValue(object model)
        {
            string description = null;

            var viewModel = model;
            if (viewModel is TemplateViewModel templateViewModel)
            {
                viewModel = templateViewModel.GetTemplateModel<object>();
            }

            if (viewModel is ISEOFields webPageWithSEO)
            {
                description = webPageWithSEO.SEOFieldsDescription;
            }
            else if (viewModel is IWebPageBasedViewModel webPageBasedViewModel)
            {
                if (webPageBasedViewModel.WebPage is ISEOFields basePageWithSEO)
                {
                    description = basePageWithSEO.SEOFieldsDescription;
                }
            }

            return description;
        }


        /// <summary>
        /// Returns value of <see cref="ISEOFields.SEOFieldsAllowSearchIndexing"/> field from the given model."/>
        /// </summary>
        /// <remarks>
        /// View model must satisfy following requirements:<br/>
        /// - is a page builder page based on <see cref="TemplateViewModel"/> class<br/>
        /// - implements <see cref="ISEOFields"/><br/>
        /// - implements <see cref="IWebPageBasedViewModel"/> and its base page <see cref="IWebPageBasedViewModel.WebPage"/> implements <see cref="ISEOFields"/><br/>
        /// <para>In other cases the method returns false.</para>
        /// </remarks>
        /// <param name="model">Model</param>
        public static bool GetSearchIndexing(object model)
        {
            bool allowSearchIndexing = false;

            var viewModel = model;
            if (viewModel is TemplateViewModel templateViewModel)
            {
                viewModel = templateViewModel.GetTemplateModel<object>();
            }

            if (viewModel is ISEOFields webPageWithSEO)
            {
                allowSearchIndexing = webPageWithSEO.SEOFieldsAllowSearchIndexing;
            }
            else if (viewModel is IWebPageBasedViewModel webPageBasedViewModel)
            {
                if (webPageBasedViewModel.WebPage is ISEOFields basePageWithSEO)
                {
                    allowSearchIndexing = basePageWithSEO.SEOFieldsAllowSearchIndexing;
                }
            }

            return allowSearchIndexing;
        }
    }
}
