using System.Diagnostics;

namespace Kentico.Xperience.Lucene.Core.Indexing;

public record IndexRetentionPolicy(int NumberOfKeptPublishedGenerations);

/// <summary>
/// Provides context and operations for managing index storage, including retrieval, generation, publishing, retention
/// enforcement, and deletion of index generations.
/// </summary>
/// <remarks>This class encapsulates the logic for interacting with index storage using a specified storage
/// strategy and retention policy. It is typically used to coordinate the lifecycle of index generations, ensuring
/// published indices are tracked and retention policies are enforced. Thread safety depends on the underlying storage
/// strategy implementation.</remarks>
public class IndexStorageContext
{
    private readonly ILuceneIndexStorageStrategy storageStrategy;
    private readonly IndexRetentionPolicy retentionPolicy;


    internal string IndexStoragePathRoot { get; }


    public IndexStorageContext(ILuceneIndexStorageStrategy selectedStorageStrategy, string indexStoragePathRoot, IndexRetentionPolicy retentionPolicy)
    {
        storageStrategy = selectedStorageStrategy;
        IndexStoragePathRoot = indexStoragePathRoot;
        this.retentionPolicy = retentionPolicy;
    }


    public IndexStorageModel GetPublishedIndex()
    {
        var published = storageStrategy
            .GetExistingIndices(IndexStoragePathRoot)
            .Where(x => x.IsPublished)
            .MaxBy(x => x.Generation);

        if (published == null)
        {
            string indexPath = storageStrategy.FormatPath(IndexStoragePathRoot, 1, false);
            string taxonomyPath = storageStrategy.FormatTaxonomyPath(IndexStoragePathRoot, 1, false);
            published = new IndexStorageModel(indexPath, taxonomyPath, 1, true);
        }

        return published;
    }

    /// <summary>
    /// Gets next generation of index
    /// </summary>
    public IndexStorageModel GetNextGeneration()
    {
        var lastIndex = storageStrategy
            .GetExistingIndices(IndexStoragePathRoot)
            .MaxBy(x => x.Generation);

        IndexStorageModel? newIndex;
        switch (lastIndex)
        {
            case var (_, _, generation, published):
                int nextGeneration = published ? generation + 1 : generation;
                string indexPath = storageStrategy.FormatPath(IndexStoragePathRoot, nextGeneration, false);
                string taxonomyPath = storageStrategy.FormatTaxonomyPath(IndexStoragePathRoot, nextGeneration, false);
                newIndex = new IndexStorageModel(indexPath, taxonomyPath, nextGeneration, false);
                break;
            default:
                newIndex = new IndexStorageModel("", "", 1, false);
                break;
        }

        return newIndex with { Path = storageStrategy.FormatPath(IndexStoragePathRoot, newIndex.Generation, newIndex.IsPublished) };
    }

    public IndexStorageModel GetLastGeneration(bool defaultPublished)
    {
        var model = storageStrategy
            .GetExistingIndices(IndexStoragePathRoot)
            .MaxBy(x => x.Generation);
        if (model == null)
        {
            string indexPath = storageStrategy.FormatPath(IndexStoragePathRoot, 1, defaultPublished);
            string taxonomyPath = storageStrategy.FormatTaxonomyPath(IndexStoragePathRoot, 1, defaultPublished);
            model = new IndexStorageModel(indexPath, taxonomyPath, 1, defaultPublished);
        }
        return model;
    }

    /// <summary>
    /// method returns last writable index storage model
    /// </summary>
    /// <returns>Storage model with information about writable index</returns>
    /// <exception cref="ArgumentException">thrown when unexpected model occurs</exception>
    public IndexStorageModel GetNextOrOpenNextGeneration()
    {
        var lastIndex = storageStrategy
            .GetExistingIndices(IndexStoragePathRoot)
            .MaxBy(x => x.Generation);

        switch (lastIndex)
        {
            case { IsPublished: false }:
                return lastIndex;
            case (_, _, var generation, true):
            {
                string indexPath = storageStrategy.FormatPath(IndexStoragePathRoot, generation + 1, false);
                string taxonomyPath = storageStrategy.FormatTaxonomyPath(IndexStoragePathRoot, generation + 1, false);
                return new IndexStorageModel(indexPath, taxonomyPath, generation + 1, false);
            }
            case null:
            {
                string indexPath = storageStrategy.FormatPath(IndexStoragePathRoot, 1, false);
                string taxonomyPath = storageStrategy.FormatTaxonomyPath(IndexStoragePathRoot, 1, false);
                // no existing index, lets create new one
                return new IndexStorageModel(indexPath, taxonomyPath, 1, false);
            }
            default:
                throw new ArgumentException($"Non-null last index storage with invalid settings '{lastIndex}'");
        }
    }

    public void PublishIndex(IndexStorageModel storage) => storageStrategy.PublishIndex(storage);

    public void EnforceRetentionPolicy()
    {
        int kept = retentionPolicy.NumberOfKeptPublishedGenerations;

        var ordered = storageStrategy
            .GetExistingIndices(IndexStoragePathRoot)
            .OrderByDescending(s => s.Generation)
            .ToArray();

        Trace.WriteLine($"C={ordered.Length}", $"IndexStorageContext.EnforceRetentionPolicy");

        for (int i = 0; i < ordered.Length; i++)
        {
            var current = ordered[i];
            if (kept > 0)
            {
                if (current.IsPublished)
                {
                    Trace.WriteLine($"I={i} K={kept} G={current.Generation} P={current.IsPublished}: keep", $"IndexStorageContext.EnforceRetentionPolicy");
                    kept -= 1;
                    continue;
                }
                else
                {
                    // ignoring last unpublished, might be just regeneration in progress
                    Trace.WriteLine($"I={i} K={kept} G={current.Generation} P={current.IsPublished}: keep", $"IndexStorageContext.EnforceRetentionPolicy");
                }
            }

            if (kept < 1)
            {
                Trace.WriteLine($"I={i} K={kept} G={current.Generation} P={current.IsPublished}: remove", $"IndexStorageContext.EnforceRetentionPolicy");
                storageStrategy.ScheduleRemoval(current);
            }
        }

        storageStrategy.PerformCleanup(IndexStoragePathRoot);
    }

    public async Task<bool> DeleteIndex() =>
        await storageStrategy.DeleteIndex(IndexStoragePathRoot);
}
