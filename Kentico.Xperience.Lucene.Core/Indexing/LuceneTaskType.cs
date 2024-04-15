using Kentico.Xperience.Lucene.Core.Indexing;

namespace Kentico.Xperience.Lucene;

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
    /// A task for a page which should be removed from the index.
    /// </summary>
    DELETE,

    /// <summary>
    /// Task marks the end of indexed items, index is published after this task occurs
    /// </summary>
    PUBLISH_INDEX,

    UPDATE
}
