using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Kentico.Xperience.Lucene.Services;

public record IndexRetentionPolicy(int NumberOfKeptPublishedGenerations);

public class IndexStorageContext
{
    private readonly IIndexStorageStrategy storageStrategy;
    private readonly string indexStoragePathRoot;
    private readonly IndexRetentionPolicy retentionPolicy;

    public IndexStorageContext(IIndexStorageStrategy selectedStorageStrategy, string indexStoragePathRoot, IndexRetentionPolicy retentionPolicy)
    {
        storageStrategy = selectedStorageStrategy;
        this.indexStoragePathRoot = indexStoragePathRoot;
        this.retentionPolicy = retentionPolicy;
    }

    public IndexStorageModel GetPublishedIndex()
    {
        
        var published = storageStrategy
            .GetExistingIndices(indexStoragePathRoot)
            .Where(x => x.IsPublished)
            .MaxBy(x => x.Generation);

        if (published == null)
        {
            string indexPath = storageStrategy.FormatPath(indexStoragePathRoot, 1, false);
            string taxonomyPath = storageStrategy.FormatTaxonomyPath(indexStoragePathRoot, 1, false);
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
            .GetExistingIndices(indexStoragePathRoot)
            .MaxBy(x => x.Generation);

        IndexStorageModel? newIndex;
        switch (lastIndex)
        {
            case var (_, _, generation, published):
                int nextGeneration = published ? generation + 1 : generation;
                string indexPath = storageStrategy.FormatPath(indexStoragePathRoot, nextGeneration, false);
                string taxonomyPath = storageStrategy.FormatTaxonomyPath(indexStoragePathRoot, nextGeneration, false);
                newIndex = new IndexStorageModel(indexPath, taxonomyPath, nextGeneration, false);
                break;
            default:
                newIndex = new IndexStorageModel("", "", 1, false);
                break;
        }

        return newIndex with { Path = storageStrategy.FormatPath(indexStoragePathRoot, newIndex.Generation, newIndex.IsPublished) };
    }

    public IndexStorageModel GetLastGeneration(bool defaultPublished)
    {
        var model = storageStrategy
            .GetExistingIndices(indexStoragePathRoot)
            .MaxBy(x => x.Generation);
        if (model == null)
        {
            string indexPath = storageStrategy.FormatPath(indexStoragePathRoot, 1, defaultPublished);
            string taxonomyPath = storageStrategy.FormatTaxonomyPath(indexStoragePathRoot, 1, defaultPublished);
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
            .GetExistingIndices(indexStoragePathRoot)
            .MaxBy(x => x.Generation);

        switch (lastIndex)
        {
            case { IsPublished: false }:
                return lastIndex;
            case (_, _, var generation, true):
            {
                string indexPath = storageStrategy.FormatPath(indexStoragePathRoot, generation + 1, false);
                string taxonomyPath = storageStrategy.FormatTaxonomyPath(indexStoragePathRoot, generation + 1, false);
                return new IndexStorageModel(indexPath, taxonomyPath, generation + 1, false);
            }
            case null:
            {
                string indexPath = storageStrategy.FormatPath(indexStoragePathRoot, 1, false);
                string taxonomyPath = storageStrategy.FormatTaxonomyPath(indexStoragePathRoot, 1, false);
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
            .GetExistingIndices(indexStoragePathRoot)
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

        storageStrategy.PerformCleanup(indexStoragePathRoot);
    }
}

public record IndexStorageModel(string Path, string TaxonomyPath, int Generation, bool IsPublished);

public interface IIndexStorageStrategy
{
    IEnumerable<IndexStorageModel> GetExistingIndices(string indexStoragePath);
    string FormatPath(string indexRoot, int generation, bool isPublished);
    string FormatTaxonomyPath(string indexRoot, int generation, bool isPublished);
    void PublishIndex(IndexStorageModel storage);
    bool ScheduleRemoval(IndexStorageModel storage);
    bool PerformCleanup(string indexStoragePath);
}

public class GenerationStorageStrategy : IIndexStorageStrategy
{
    private const string IndexDeletionDirectoryName = ".trash";

    public IEnumerable<IndexStorageModel> GetExistingIndices(string indexStoragePath)
    {
        if (!Directory.Exists(indexStoragePath))
        {
            yield break;
        }

        var grouped = Directory.GetDirectories(indexStoragePath)
            .Select(ParseIndexStorageModel)
            .Where(x => x.Success)
            .GroupBy(x => x.Result?.Generation ?? -1);


        foreach (var result in grouped)
        {
            var indexDir = result.FirstOrDefault(x => string.IsNullOrWhiteSpace(x.Result?.TaxonomyName));
            var taxonomyDir = result.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Result?.TaxonomyName));

            if (indexDir is { Success: true, Result: var (indexPath, generation, published, _) })
            {
                string taxonomyPath = taxonomyDir?.Result?.Path ?? FormatTaxonomyPath(indexStoragePath, generation, false);
                yield return new IndexStorageModel(indexPath, taxonomyPath, generation, published);
            }
        }
    }

    public string FormatPath(string indexRoot, int generation, bool isPublished) => Path.Combine(indexRoot, $"i-g{generation:0000000}-p_{isPublished}");
    public string FormatTaxonomyPath(string indexRoot, int generation, bool isPublished) => Path.Combine(indexRoot, $"i-g{generation:0000000}-p_{isPublished}_taxonomy");

    public void PublishIndex(IndexStorageModel storage)
    {
        string root = Path.Combine(storage.Path, "..");
        var published = storage with
        {
            IsPublished = true,
            Path = FormatPath(root, storage.Generation, true),
            TaxonomyPath = FormatTaxonomyPath(root, storage.Generation, true)
        };

        Directory.Move(storage.Path, published.Path);

        if (Directory.Exists(storage.TaxonomyPath))
        {
            Directory.Move(storage.TaxonomyPath, published.TaxonomyPath);
        }
    }

    public bool ScheduleRemoval(IndexStorageModel storage)
    {
        (string? path, string? taxonomyPath, int generation, bool _) = storage;

        string delBase = Path.Combine(path, $@"..\{IndexDeletionDirectoryName}");
        Directory.CreateDirectory(delBase);

        string delPath = Path.Combine(path, $@"..\{IndexDeletionDirectoryName}\{generation:0000000}");
        try
        {
            Directory.Move(path, delPath);
            Trace.WriteLine($"OP={path} NP={delPath}: removal scheduled", $"GenerationStorageStrategy.ScheduleRemoval");
        }
        catch (IOException ioex)
        {
            Trace.WriteLine($"OP={path} NP={delPath}: {ioex}", $"GenerationStorageStrategy.ScheduleRemoval");
            // fail, directory is possibly locked by reader
            return false;
        }

        if (!string.IsNullOrWhiteSpace(taxonomyPath) && Directory.Exists(taxonomyPath))
        {
            string delPathTaxon = Path.Combine(path, $@"..\{IndexDeletionDirectoryName}\{generation:0000000}_taxon");
            try
            {
                Directory.Move(taxonomyPath, delPathTaxon);
                Trace.WriteLine($"OP={taxonomyPath} NP={delPathTaxon}: removal scheduled", $"GenerationStorageStrategy.ScheduleRemoval");
            }
            catch (IOException ioex)
            {
                // fail, directory is possibly locked by reader
                Trace.WriteLine($"OP={taxonomyPath} NP={delPathTaxon}: {ioex}", $"GenerationStorageStrategy.ScheduleRemoval");

                // restore index
                Directory.Move(delPath, path);
                return false;
            }
        }

        return true;
    }

    public bool PerformCleanup(string indexStoragePath)
    {
        string toDeleteDir = Path.Combine(indexStoragePath, IndexDeletionDirectoryName);
        var thrashDir = new DirectoryInfo(toDeleteDir);
        try
        {
            if (!thrashDir.Exists)
            {
                return true;
            }

            foreach (var file in thrashDir.GetFiles())
            {
                try
                {
                    Trace.WriteLine($"F={file.Name}: delete", $"GenerationStorageStrategy.PerformCleanup");
                    file.Delete();
                }
                catch
                {
                    // ignored, can't do anything about resource - next iteration will pick resource to delete again
                }
            }

            foreach (var dir in thrashDir.GetDirectories())
            {
                try
                {
                    Trace.WriteLine($"D={dir.Name}: delete *.*", $"GenerationStorageStrategy.PerformCleanup");
                    dir.Delete(true);
                }
                catch
                {
                    // ignored, can't do anything about resource - next iteration will pick resource to delete again
                }
            }
        }
        catch (IOException)
        {
            // directory might be destroyed or inaccessible

            return false;
        }

        return true;
    }

    public record IndexStorageModelParseResult(string Path, int Generation, bool IsPublished, string? TaxonomyName);
    private sealed record IndexStorageModelParsingResult(
        bool Success,
        [property: MemberNotNullWhen(true, "Success")] IndexStorageModelParseResult? Result
    );

    private IndexStorageModelParsingResult ParseIndexStorageModel(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            return new IndexStorageModelParsingResult(false, null);
        }

        try
        {
            var dirInfo = new DirectoryInfo(directoryPath);
            if (dirInfo.Name is { Length: > 0 } directoryName)
            {
                var matchResult = Regex.Match(directoryName, "i-g(?<generation>[0-9]*)-p_(?<published>(true)|(false))(_(?<taxonomy>[a-z0-9]*)){0,1}", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                switch (matchResult)
                {
                    case { Success: true } r
                        when r.Groups["generation"] is { Success: true, Value: { Length: > 0 } gen } &&
                             r.Groups["published"] is { Success: true, Value: { Length: > 0 } pub }:
                    {
                        string? taxonomyName = null;
                        if (r.Groups["taxonomy"] is { Success: true, Value: { Length: > 0 } taxonomy })
                        {
                            taxonomyName = taxonomy;
                        }

                        if (int.TryParse(gen, out int generation) && bool.TryParse(pub, out bool published))
                        {
                            return new IndexStorageModelParsingResult(true, new IndexStorageModelParseResult(directoryPath, generation, published, taxonomyName));
                        }

                        break;
                    }
                    default:
                    {
                        return new IndexStorageModelParsingResult(false, null);
                    }
                }
            }
        }
        catch
        {
            // low priority, if path cannot be parsed, it is possibly not generated index
            // ignored
        }

        return new IndexStorageModelParsingResult(false, null);
    }
}
