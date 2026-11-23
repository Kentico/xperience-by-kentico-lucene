using System.Collections.Concurrent;

namespace Kentico.Xperience.Lucene.Core.Indexing;

/// <summary>
/// The last rebuild request recorded for an index on this application instance.
/// </summary>
/// <param name="RequestedAtUtc">When the rebuild was requested.</param>
/// <param name="QueuedItemCount">How many items the rebuild put on the indexing queue.</param>
public sealed record LuceneRebuildRecord(DateTime RequestedAtUtc, int QueuedItemCount);


/// <summary>
/// Remembers the most recent rebuild request per index. This is an in-memory record of what was requested
/// and queued only: it does not know whether queued items were processed, and it is lost on restart or
/// not shared between web farm servers.
/// </summary>
public sealed class LuceneRebuildTracker
{
    private readonly ConcurrentDictionary<string, LuceneRebuildRecord> records = new(StringComparer.OrdinalIgnoreCase);


    internal void RecordRequest(string indexName, int queuedItemCount) =>
        records[indexName] = new LuceneRebuildRecord(DateTime.UtcNow, queuedItemCount);


    /// <summary>
    /// Gets the last rebuild request for the index, or <see langword="null"/> when none was made since startup.
    /// </summary>
    public LuceneRebuildRecord? GetLastRequest(string indexName) =>
        records.TryGetValue(indexName, out var record) ? record : null;
}
