namespace Kentico.Xperience.Lucene.Core.Indexing;

/// <summary>
/// Keeps the full results of a few recent diagnostic runs so their categories can be paged
/// without mapping every item through the indexing strategy again.
/// </summary>
internal sealed class LuceneDiagnosticsSnapshotStore
{
    internal const int MaxSnapshots = 4;
    internal static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);

    private readonly Lock sync = new();
    private readonly List<Snapshot> snapshots = [];
    private readonly TimeProvider timeProvider;


    public LuceneDiagnosticsSnapshotStore(TimeProvider? timeProvider = null) =>
        this.timeProvider = timeProvider ?? TimeProvider.System;


    public Guid Add(
        string indexName,
        IReadOnlyList<LuceneDiagnosticItem> skipped,
        IReadOnlyList<LuceneDiagnosticItem> missing,
        IReadOnlyList<LuceneDiagnosticItem> orphans)
    {
        var now = timeProvider.GetUtcNow();
        var snapshot = new Snapshot(Guid.NewGuid(), indexName, now, skipped, missing, orphans);

        lock (sync)
        {
            RemoveExpired(now);
            snapshots.Add(snapshot);
            if (snapshots.Count > MaxSnapshots)
            {
                snapshots.RemoveRange(0, snapshots.Count - MaxSnapshots);
            }
        }

        return snapshot.RunId;
    }


    public IReadOnlyList<LuceneDiagnosticItem>? Get(Guid runId, string indexName, LuceneDiagnosticCategory category)
    {
        lock (sync)
        {
            RemoveExpired(timeProvider.GetUtcNow());
            var snapshot = snapshots.Find(s => s.RunId == runId && string.Equals(s.IndexName, indexName, StringComparison.OrdinalIgnoreCase));

            return category switch
            {
                LuceneDiagnosticCategory.Skipped => snapshot?.Skipped,
                LuceneDiagnosticCategory.Missing => snapshot?.Missing,
                LuceneDiagnosticCategory.Orphans => snapshot?.Orphans,
                _ => null,
            };
        }
    }


    private void RemoveExpired(DateTimeOffset now) =>
        snapshots.RemoveAll(s => now - s.CreatedAt > Lifetime);


    private sealed record Snapshot(
        Guid RunId,
        string IndexName,
        DateTimeOffset CreatedAt,
        IReadOnlyList<LuceneDiagnosticItem> Skipped,
        IReadOnlyList<LuceneDiagnosticItem> Missing,
        IReadOnlyList<LuceneDiagnosticItem> Orphans);
}
