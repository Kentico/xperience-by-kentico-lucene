using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace DancingGoat.ViewComponents
{
    /// <summary>
    /// Generates JSON-LD structured data (schema.org) for view models it supports.
    /// Implementations are registered in the IoC container and evaluated for every rendered page.
    /// </summary>
    public interface IJsonLdGenerator
    {
        /// <summary>
        /// Returns true when the generator can produce structured data for the given view model.
        /// </summary>
        /// <param name="viewModel">View model of the rendered page.</param>
        bool SupportsModel(object viewModel);


        /// <summary>
        /// Generates JSON-LD objects for the given view model.
        /// </summary>
        /// <param name="viewModel">View model of the rendered page.</param>
        /// <param name="context">Resolved SEO metadata of the current page.</param>
        /// <param name="cancellationToken">Cancellation instruction.</param>
        Task<IEnumerable<JsonObject>> Generate(object viewModel, SeoMetadataContext context, CancellationToken cancellationToken);
    }
}
