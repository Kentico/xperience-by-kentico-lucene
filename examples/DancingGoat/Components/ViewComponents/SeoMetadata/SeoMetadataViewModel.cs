using System.Collections.Generic;

namespace DancingGoat.ViewComponents
{
    /// <summary>
    /// View model with SEO metadata rendered into the page head.
    /// </summary>
    /// <param name="Title">Page title.</param>
    /// <param name="Description">Meta description.</param>
    /// <param name="CanonicalUrl">Absolute canonical URL of the page.</param>
    /// <param name="AllowIndexing">Indicates whether search engines are allowed to index the page.</param>
    /// <param name="OpenGraphImageUrl">Absolute URL of the OpenGraph image.</param>
    /// <param name="OpenGraphType">OpenGraph object type (e.g. website, article, product).</param>
    /// <param name="JsonLdPayloads">Serialized JSON-LD structured data payloads.</param>
    public record SeoMetadataViewModel(
        string Title,
        string Description,
        string CanonicalUrl,
        bool AllowIndexing,
        string OpenGraphImageUrl,
        string OpenGraphType,
        IReadOnlyList<string> JsonLdPayloads)
    {
    }
}
