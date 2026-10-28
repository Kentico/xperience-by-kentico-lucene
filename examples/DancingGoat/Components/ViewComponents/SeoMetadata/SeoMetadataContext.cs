using System;

using CMS.Websites;

namespace DancingGoat.ViewComponents
{
    /// <summary>
    /// Resolved SEO metadata of the current page passed to <see cref="IJsonLdGenerator"/> implementations.
    /// </summary>
    /// <param name="Title">Resolved page title.</param>
    /// <param name="Description">Resolved meta description.</param>
    /// <param name="CanonicalUrl">Absolute canonical URL of the page.</param>
    /// <param name="BaseUri">Base URI of the current request.</param>
    /// <param name="LanguageName">Language of the current request.</param>
    /// <param name="WebPage">Web page the current view model is based on. Can be null on non-content routes.</param>
    public record SeoMetadataContext(
        string Title,
        string Description,
        string CanonicalUrl,
        Uri BaseUri,
        string LanguageName,
        IWebPageFieldsSource WebPage)
    {
        /// <summary>
        /// Converts an application-relative or server-relative path to an absolute URL based on <see cref="BaseUri"/>.
        /// </summary>
        /// <param name="path">Path to convert. Absolute URLs are returned unchanged.</param>
        public string GetAbsoluteUrl(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            if (Uri.TryCreate(path, UriKind.Absolute, out var absoluteUri))
            {
                return absoluteUri.ToString();
            }

            return new Uri(BaseUri, path.TrimStart('~')).ToString();
        }
    }
}
