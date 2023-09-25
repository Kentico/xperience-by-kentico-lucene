using Microsoft.AspNetCore.Routing;

namespace DancingGoat
{
    /// <summary>
    /// Retrieves active language.
    /// </summary>
    public interface ICurrentLanguageRetriever
    {
        /// <summary>
        /// Retrieves the active language on the site from <see cref="RouteValueDictionary"/>.
        /// </summary>
        /// <remarks>
        /// Primary language of a current website channel is returned if no language is set in <see cref="RouteValueDictionary"/>.
        /// </remarks>
        public string Get();
    }
}
