using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

using CMS.Core;

namespace Kentico.Xperience.Lucene.Core.Indexing;

public interface ILuceneIndexStorageStrategy
{
    /// <summary>
    /// Gets all existing indices.
    /// </summary>
    /// <param name="indexStoragePath">Path root of the index storage.</param>
    IEnumerable<IndexStorageModel> GetExistingIndices(string indexStoragePath);

    /// <summary>
    /// Formats path of an index in a specified generation.
    /// </summary>
    /// <param name="indexRoot">Root path of the index.</param>
    /// <param name="generation">Indexing generation.</param>
    /// <param name="isPublished">Parameter specifying whether the index has been published.</param>
    string FormatPath(string indexRoot, int generation, bool isPublished);

    /// <summary>
    /// Formats path of a taxonomy of an index in a specified generation.
    /// </summary>
    /// <param name="indexRoot">Root path of the index.</param>
    /// <param name="generation">Indexing generation.</param>
    /// <param name="isPublished">Parameter specifying whether the taxonomy has been published.</param>
    string FormatTaxonomyPath(string indexRoot, int generation, bool isPublished);

    /// <summary>
    /// Publishes the index.
    /// </summary>
    /// <param name="storage">Index storage model.</param>
    void PublishIndex(IndexStorageModel storage);

    /// <summary>
    /// Schedules removal of files of an index.
    /// </summary>
    /// <param name="storage">Index storage model.</param>
    bool ScheduleRemoval(IndexStorageModel storage);

    /// <summary>
    /// Performs cleanup of files of an index.
    /// </summary>
    /// <param name="indexStoragePath">Path root of the index storage.</param>
    bool PerformCleanup(string indexStoragePath);

    /// <summary>
    /// Deletes all files associated with an index.
    /// </summary>
    /// <param name="indexStoragePath">Path root of the index storage.</param>
    Task<bool> DeleteIndex(string indexStoragePath);
}

internal class GenerationStorageStrategy : ILuceneIndexStorageStrategy
{
    private const string IndexDeletionDirectoryName = ".trash";
    private readonly IEventLogService eventLogService;

    public GenerationStorageStrategy() =>
        eventLogService = Service.Resolve<IEventLogService>();

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

        string delBase = Path.Combine(path, "..", IndexDeletionDirectoryName);
        Directory.CreateDirectory(delBase);

        string delPath = Path.Combine(path, "..", IndexDeletionDirectoryName, $"{generation:0000000}");
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
            string delPathTaxon = Path.Combine(path, "..", IndexDeletionDirectoryName, $"{generation:0000000}_taxon");
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

    public async Task<bool> DeleteIndex(string indexStoragePath)
    {
        var deleteDir = new DirectoryInfo(indexStoragePath);

        if (!deleteDir.Exists)
        {
            return true;
        }

        int numberOfRetries = 10;
        int millisecondsRetryDelay = 100;
        int millisecondsAddedToRetryPerRequest = millisecondsRetryDelay * 2;

        for (int i = 0; i < numberOfRetries; i++)
        {
            try
            {
                Trace.WriteLine($"D={deleteDir.Name}: delete *.*", $"GenerationStorageStrategy.DeleteIndex");
                deleteDir.Delete(true);
                return true;
            }
            catch
            {
                // Do nothing with exception and retry.
                // The directory may be locked by another process, but we can not know about it without trying to delete it.
                // The exact exception is not known and is not written in .NET documentation.

                await Task.Delay(millisecondsRetryDelay + (millisecondsAddedToRetryPerRequest * numberOfRetries));
            }
        }
        try
        {
            Trace.WriteLine($"D={deleteDir.Name}: delete *.*", $"GenerationStorageStrategy.DeleteIndex");
            deleteDir.Delete(true);
        }
        catch (Exception ex)
        {
            eventLogService.LogError(nameof(GenerationStorageStrategy), nameof(DeleteIndex), ex.Message);
            return false;
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

    internal record IndexStorageModelParseResult(string Path, int Generation, bool IsPublished, string? TaxonomyName);
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
