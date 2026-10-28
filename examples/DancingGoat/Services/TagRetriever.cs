using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CMS.ContentEngine;

namespace DancingGoat.Services
{
    /// <summary>
    /// Tag retriever to get a taxonomy tag.
    /// </summary>
    public sealed class TagRetriever
    {
        private readonly ITaxonomyRetriever taxonomyRetriever;


        public TagRetriever(ITaxonomyRetriever taxonomyRetriever)
        {
            this.taxonomyRetriever = taxonomyRetriever;
        }


        /// <summary>
        /// Get a tag based on the tag identifier.
        /// </summary>
        /// <param name="tagIdentifier">Tag identifier to retrieve from database.</param>
        /// <param name="languageName">Language name.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Retrieved tag if it exists, null otherwise.</returns>
        public async Task<Tag> GetTag(Guid tagIdentifier, string languageName, CancellationToken cancellationToken)
        {
            var tags = await taxonomyRetriever.RetrieveTags([tagIdentifier], languageName, cancellationToken);

            return tags.FirstOrDefault();
        }


        /// <summary>
        /// Get tags based on the tag identifiers.
        /// </summary>
        /// <param name="tagIdentifiers">Tag identifiers to retrieve from database.</param>
        /// <param name="languageName">Language name.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Retrieved tags, an empty collection when no identifier is provided.</returns>
        public async Task<IEnumerable<Tag>> GetTags(IEnumerable<Guid> tagIdentifiers, string languageName, CancellationToken cancellationToken)
        {
            var identifiers = tagIdentifiers.ToList();
            if (identifiers.Count == 0)
            {
                return [];
            }

            return await taxonomyRetriever.RetrieveTags(identifiers, languageName, cancellationToken);
        }
    }
}
