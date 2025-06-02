using Lucene.Net.Documents;

namespace Kentico.Xperience.Lucene.Core.Indexing;

/// <summary>
/// Represents the result of preprocessing data for a Lucene index operation.
/// </summary>
/// <remarks>This class encapsulates the data required for managing Lucene index updates, including identifiers
/// for documents to delete, documents to upsert, and a flag indicating whether the index should be published.</remarks>
public class LucenePreprocessResult
{
    /// <summary>
    /// The list of IDs to be deleted.
    /// </summary>
    internal List<string> DeleteIds { get; set; } = [];

    /// <summary>
    /// The collection of documents to be inserted or updated in the data store.
    /// </summary>
    internal List<Document> UpsertData { get; set; } = [];

    /// <summary>
    /// A value indicating whether the index should be published.
    /// </summary>
    internal bool PublishIndex { get; set; } = false;
}
