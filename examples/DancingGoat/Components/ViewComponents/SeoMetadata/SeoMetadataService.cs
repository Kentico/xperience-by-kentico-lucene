using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CMS.Websites;
using CMS.Websites.Routing;

using DancingGoat.Models;

using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.PageBuilder;

namespace DancingGoat.ViewComponents
{
    /// <summary>
    /// Resolves SEO metadata for the currently rendered page from its view model and the reusable "SEO fields" schema.
    /// </summary>
    public sealed class SeoMetadataService
    {
        private readonly IWebPageDataContextRetriever webPageDataContextRetriever;
        private readonly IWebPageUrlRetriever webPageUrlRetriever;
        private readonly IWebsiteChannelContext websiteChannelContext;
        private readonly IEnumerable<IJsonLdGenerator> jsonLdGenerators;


        /// <summary>
        /// Initializes a new instance of the <see cref="SeoMetadataService"/> class.
        /// </summary>
        public SeoMetadataService(
            IWebPageDataContextRetriever webPageDataContextRetriever,
            IWebPageUrlRetriever webPageUrlRetriever,
            IWebsiteChannelContext websiteChannelContext,
            IEnumerable<IJsonLdGenerator> jsonLdGenerators)
        {
            this.webPageDataContextRetriever = webPageDataContextRetriever;
            this.webPageUrlRetriever = webPageUrlRetriever;
            this.websiteChannelContext = websiteChannelContext;
            this.jsonLdGenerators = jsonLdGenerators;
        }


        /// <summary>
        /// Resolves the SEO metadata for the given page view model.
        /// </summary>
        /// <param name="pageModel">View model of the rendered page. Page builder template models are unwrapped automatically.</param>
        /// <param name="fallbackTitle">Title used when the view model does not provide an SEO title.</param>
        /// <param name="baseUri">Base URI of the current request used to build absolute URLs.</param>
        /// <param name="cancellationToken">Cancellation instruction.</param>
        public async Task<SeoMetadataViewModel> GetSeoMetadata(object pageModel, string fallbackTitle, Uri baseUri, CancellationToken cancellationToken)
        {
            var viewModel = UnwrapTemplateModel(pageModel);
            var seoFields = GetSeoFields(viewModel);
            var webPage = GetWebPage(viewModel);

            var seoTitle = seoFields?.SEOFieldsTitle;
            var title = string.IsNullOrEmpty(seoTitle) ? fallbackTitle : seoTitle;
            var description = seoFields?.SEOFieldsDescription;

            // Pages without the "SEO fields" schema are deliberately not indexable; an unset No-index field (NULL binds to false) keeps schema pages indexable
            var allowIndexing = seoFields != null && !seoFields.SEOFieldsNoIndex;

            var canonicalUrl = await GetCanonicalUrl(cancellationToken);
            var context = new SeoMetadataContext(title, description, canonicalUrl, baseUri, GetLanguageName(), webPage);
            var imageUrl = context.GetAbsoluteUrl(GetImagePath(seoFields, viewModel));

            var jsonLdPayloads = await GetJsonLdPayloads(viewModel, context, cancellationToken);

            return new SeoMetadataViewModel(title, description, canonicalUrl, allowIndexing, imageUrl, GetOpenGraphType(viewModel), jsonLdPayloads);
        }


        private static object UnwrapTemplateModel(object pageModel)
        {
            if (pageModel is TemplateViewModel templateViewModel)
            {
                return templateViewModel.GetTemplateModel<object>();
            }

            return pageModel;
        }


        private static ISEOFields GetSeoFields(object viewModel)
        {
            if (viewModel is ISEOFields seoFields)
            {
                return seoFields;
            }

            if (viewModel is IWebPageBasedViewModel webPageBasedViewModel)
            {
                return webPageBasedViewModel.WebPage as ISEOFields;
            }

            return null;
        }


        private static IWebPageFieldsSource GetWebPage(object viewModel)
        {
            if (viewModel is IWebPageBasedViewModel webPageBasedViewModel)
            {
                return webPageBasedViewModel.WebPage;
            }

            return viewModel as IWebPageFieldsSource;
        }


        private static string GetImagePath(ISEOFields seoFields, object viewModel)
        {
            var openGraphImagePath = seoFields?.SEOFieldsOGImage?.FirstOrDefault()?.ImageFile?.Url;
            if (!string.IsNullOrEmpty(openGraphImagePath))
            {
                return openGraphImagePath;
            }

            return viewModel switch
            {
                ProductViewModel product => product.ImagePath,
                ArticleDetailViewModel article => article.TeaserUrl,
                _ => null
            };
        }


        private static string GetOpenGraphType(object viewModel)
        {
            return viewModel switch
            {
                ProductViewModel => "product",
                ArticleDetailViewModel => "article",
                _ => "website"
            };
        }


        private string GetLanguageName()
        {
            if (webPageDataContextRetriever.TryRetrieve(out var webPageDataContext))
            {
                return webPageDataContext.WebPage.LanguageName;
            }

            return null;
        }


        private async Task<string> GetCanonicalUrl(CancellationToken cancellationToken)
        {
            if (!webPageDataContextRetriever.TryRetrieve(out var webPageDataContext))
            {
                return null;
            }

            var routedWebPage = webPageDataContext.WebPage;
            var url = await webPageUrlRetriever.Retrieve(routedWebPage.WebPageItemID, routedWebPage.LanguageName,
                websiteChannelContext.IsPreview, cancellationToken);

            return url.AbsoluteUrl;
        }


        private async Task<IReadOnlyList<string>> GetJsonLdPayloads(object viewModel, SeoMetadataContext context, CancellationToken cancellationToken)
        {
            var jsonLdPayloads = new List<string>();

            foreach (var jsonLdGenerator in jsonLdGenerators)
            {
                if (!jsonLdGenerator.SupportsModel(viewModel))
                {
                    continue;
                }

                foreach (var jsonLdObject in await jsonLdGenerator.Generate(viewModel, context, cancellationToken))
                {
                    jsonLdPayloads.Add(jsonLdObject.ToJsonString());
                }
            }

            return jsonLdPayloads;
        }
    }
}
