using System.Threading;
using System.Threading.Tasks;

namespace DancingGoat
{
    /// <summary>
    /// Retrieves current website channel primary language.
    /// </summary>
    public interface ICurrentWebsiteChannelPrimaryLanguageRetriever
    {
        /// <summary>
        /// Returns language code of the current website channel primary language.
        /// </summary>
        /// <param name="cancellationToken">Cancellation instruction.</param>
        public Task<string> Get(CancellationToken cancellationToken = default);
    }
}
