using Kentico.Xperience.Lucene.Models;

namespace Kentico.Xperience.Lucene
{
    /// <summary>
    /// Represents the type of a <see cref="LuceneQueueItem"/>.
    /// </summary>
    public enum LuceneTaskType
    {
        /// <summary>
        /// Unsupported task type.
        /// </summary>
        UNKNOWN,

        /// <summary>
        /// A task for a page which was published for the first time.
        /// </summary>
        CREATE,

        /// <summary>
        /// A task for a page which was previously published.
        /// </summary>
        UPDATE,

        /// <summary>
        /// A task for a page which should be removed from the index.
        /// </summary>
        DELETE
    }
}
