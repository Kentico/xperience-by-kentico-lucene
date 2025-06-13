using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CMS.ContentEngine;

namespace DancingGoat.Services
{
    /// <summary>
    /// Tag title retriever to get title of a tag.
    /// </summary>
    public sealed class TagTitleRetriever
    {
        private readonly ITaxonomyRetriever taxonomyRetriever;


        public TagTitleRetriever(ITaxonomyRetriever taxonomyRetriever)
        {
            this.taxonomyRetriever = taxonomyRetriever;
        }


        /// <summary>
        /// Get title of a tag based on the tag identifier.
        /// </summary>
        /// <param name="tagIdentifier">Tag identifier to retrieve from database.</param>
        /// <param name="languageName">Language name.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Received title if tag exists, null otherwise.</returns>
        public async Task<string> GetTagTitle(Guid tagIdentifier, string languageName, CancellationToken cancellationToken)
        {
            var tags = await taxonomyRetriever.RetrieveTags([tagIdentifier], languageName, cancellationToken);
            return tags.FirstOrDefault()?.Title;
        }
    }
}
