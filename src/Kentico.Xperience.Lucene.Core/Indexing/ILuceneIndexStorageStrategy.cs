using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

using CMS.Core;
using CMS.Helpers.Synchronization;

using Microsoft.Extensions.Hosting;

using CmsDirectory = CMS.IO.Directory;
using CmsDirectoryInfo = CMS.IO.DirectoryInfo;
using CmsPath = CMS.IO.Path;

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
    private readonly IHostEnvironment environment;

    public GenerationStorageStrategy()
    {
        eventLogService = Service.Resolve<IEventLogService>();
        environment = Service.Resolve<IHostEnvironment>();
    }

    public IEnumerable<IndexStorageModel> GetExistingIndices(string indexStoragePath)
    {
        if (!CmsDirectory.Exists(indexStoragePath))
        {
            yield break;
        }

        var grouped = CmsDirectory.GetDirectories(indexStoragePath)
            .Select(ParseIndexStorageModel)
            .Where(x => x.Success)
            .GroupBy(x => x.Result?.Generation ?? -1);

        foreach (var result in grouped)
        {
            var indexDir = result.FirstOrDefault(x => string.IsNullOrWhiteSpace(x.Result?.TaxonomyName));
            var taxonomyDir = result.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Result?.TaxonomyName));

            if (indexDir is { Success: true, Result: var (_, generation, published, _) })
            {
                // The taxonomy folder carries its own published flag, which normally matches the index
                // folder's. Mirror the index folder when it is missing - formatting it as unpublished would
                // point a published generation at the directory a rebuild writes into (and then renames).
                string taxonomyPath = taxonomyDir is { Success: true, Result: var (_, _, taxonomyPublished, _) }
                    ? FormatTaxonomyPath(indexStoragePath, generation, taxonomyPublished)
                    : FormatTaxonomyPath(indexStoragePath, generation, published);
                string relativePath = FormatPath(indexStoragePath, generation, published);
                yield return new IndexStorageModel(relativePath, taxonomyPath, generation, published);
            }
        }
    }

    public string FormatPath(string indexRoot, int generation, bool isPublished) =>
        CmsPath.Combine(indexRoot, $"i-g{generation:0000000}-p_{isPublished}").Replace('\\', '/');

    public string FormatTaxonomyPath(string indexRoot, int generation, bool isPublished) =>
        CmsPath.Combine(indexRoot, $"i-g{generation:0000000}-p_{isPublished}_taxonomy").Replace('\\', '/');

    public void PublishIndex(IndexStorageModel storage)
    {
        bool lockAcquired = false;
        var fileLock = new FileLock(LuceneIndexLockHelper.GetLockFilePath(storage.Path, environment));

        try
        {
            lockAcquired = fileLock.WaitForLock(LuceneIndexLockHelper.LOCK_WAIT_TIMEOUT);

            string root = CmsPath.Combine(storage.Path, "..");
            var published = storage with
            {
                IsPublished = true,
                Path = FormatPath(root, storage.Generation, true),
                TaxonomyPath = FormatTaxonomyPath(root, storage.Generation, true)
            };

            MoveWithRetry(storage.Path, published.Path);

            if (CmsDirectory.Exists(storage.TaxonomyPath))
            {
                try
                {
                    MoveWithRetry(storage.TaxonomyPath, published.TaxonomyPath);
                }
                catch (IOException)
                {
                    // Restore the index folder so the generation stays consistently unpublished and can be
                    // published again later. Leaving it renamed would pair a published index folder with an
                    // unpublished taxonomy folder, which no path resolution can express.
                    try
                    {
                        CmsDirectory.Move(published.Path, storage.Path);
                    }
                    catch (IOException rollbackEx)
                    {
                        eventLogService.LogError(nameof(GenerationStorageStrategy), nameof(PublishIndex),
                            $"Could not restore the index folder '{published.Path}' to '{storage.Path}' after a failed taxonomy publish. {rollbackEx.Message}");
                    }

                    throw;
                }
            }
        }
        finally
        {
            if (lockAcquired)
            {
                fileLock.Release();
            }
        }
    }

    /// <summary>
    /// Renames a generation folder, retrying briefly when the source is still reported as locked.
    /// </summary>
    /// <remarks>
    /// A cached search reader holds OS handles inside the folder, and on Windows that blocks the rename with
    /// an <see cref="IOException"/> ("Access to the path ... is denied"). Callers now invalidate the search
    /// cache and wait for its readers to be disposed before publishing, so this is only a backstop for
    /// handles this process does not own - a reader on another web farm server, an antivirus scanner, or an
    /// open writer. Only a lock-shaped failure is retried; anything else fails on the first attempt.
    /// </remarks>
    private void MoveWithRetry(string source, string destination)
    {
        const int numberOfAttempts = 3;
        const int millisecondsRetryDelay = 100;

        for (int attempt = 1; attempt <= numberOfAttempts; attempt++)
        {
            try
            {
                CmsDirectory.Move(source, destination);
                return;
            }
            catch (IOException ioex)
            {
                if (attempt == numberOfAttempts || !IsFolderLocked(ioex))
                {
                    eventLogService.LogError(nameof(GenerationStorageStrategy), nameof(PublishIndex),
                        $"Could not move '{source}' to '{destination}'. {ioex.Message}");
                    throw;
                }

                Trace.WriteLine($"OP={source} NP={destination} A={attempt}: {ioex.Message}", $"GenerationStorageStrategy.{nameof(MoveWithRetry)}");
                Thread.Sleep(millisecondsRetryDelay * attempt);
            }
        }
    }


    /// <summary>
    /// Indicates whether the exception is the operating system reporting the folder as held by another
    /// handle - the only failure a retry can help with.
    /// </summary>
    /// <remarks>
    /// Retrying anything else only wastes time and, on Azure Blob Storage, transactions: a missing folder, a
    /// full disk or a throttled request fails identically on every attempt, and a partially completed
    /// copy-and-delete (which is how a "rename" is implemented there) is not safe to repeat blindly. Note
    /// that <see cref="FileNotFoundException"/> and <see cref="DirectoryNotFoundException"/> both derive from
    /// <see cref="IOException"/>, so they have to be excluded explicitly.
    /// </remarks>
    private static bool IsFolderLocked(IOException ex)
    {
        const int errorAccessDenied = 0x05;
        const int errorSharingViolation = 0x20;
        const int errorLockViolation = 0x21;

        if (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            return false;
        }

        return (ex.HResult & 0xFFFF) is errorAccessDenied or errorSharingViolation or errorLockViolation;
    }

    public bool ScheduleRemoval(IndexStorageModel storage)
    {
        bool pathLockAcquired = false;

        var fileLock = new FileLock(LuceneIndexLockHelper.GetLockFilePath(storage.Path, environment));

        try
        {
            pathLockAcquired = fileLock.WaitForLock(LuceneIndexLockHelper.LOCK_WAIT_TIMEOUT);

            (string? path, string? taxonomyPath, int generation, bool _) = storage;

            string delBase = CmsPath.Combine(path, "..", IndexDeletionDirectoryName);
            if (!CmsDirectory.Exists(delBase))
            {
                CmsDirectory.CreateDirectory(delBase);
            }

            string delPath = CmsPath.Combine(path, "..", IndexDeletionDirectoryName, $"{generation:0000000}");
            try
            {
                CmsDirectory.Move(path, delPath);
                Trace.WriteLine($"OP={path} NP={delPath}: removal scheduled", $"GenerationStorageStrategy.ScheduleRemoval");
            }
            catch (IOException ioex)
            {
                Trace.WriteLine($"OP={path} NP={delPath}: {ioex}", $"GenerationStorageStrategy.ScheduleRemoval");
                // fail, directory is possibly locked by reader
                return false;
            }

            if (!string.IsNullOrWhiteSpace(taxonomyPath) && CmsDirectory.Exists(taxonomyPath))
            {
                string delPathTaxon = CmsPath.Combine(path, "..", IndexDeletionDirectoryName, $"{generation:0000000}_taxon");
                try
                {
                    CmsDirectory.Move(taxonomyPath, delPathTaxon);
                    Trace.WriteLine($"OP={taxonomyPath} NP={delPathTaxon}: removal scheduled", $"GenerationStorageStrategy.ScheduleRemoval");
                }
                catch (IOException ioex)
                {
                    // fail, directory is possibly locked by reader
                    Trace.WriteLine($"OP={taxonomyPath} NP={delPathTaxon}: {ioex}", $"GenerationStorageStrategy.ScheduleRemoval");

                    // restore index
                    CmsDirectory.Move(delPath, path);
                    return false;
                }
            }

            return true;
        }
        finally
        {
            if (pathLockAcquired)
            {
                fileLock.Release();
            }
        }
    }

    public async Task<bool> DeleteIndex(string indexStoragePath)
    {
        if (!CmsDirectory.Exists(indexStoragePath))
        {
            return true;
        }

        bool lockAcquired = false;
        var fileLock = new FileLock(LuceneIndexLockHelper.GetLockFilePath(indexStoragePath, environment));

        try
        {
            lockAcquired = fileLock.WaitForLock(LuceneIndexLockHelper.LOCK_WAIT_TIMEOUT);

            int numberOfRetries = 10;
            int millisecondsRetryDelay = 100;
            int millisecondsAddedToRetryPerRequest = millisecondsRetryDelay * 2;

            for (int i = 0; i < numberOfRetries; i++)
            {
                try
                {
                    Trace.WriteLine($"D={CmsPath.GetFileName(indexStoragePath)}: delete *.*", $"GenerationStorageStrategy.DeleteIndex");
                    CmsDirectory.Delete(indexStoragePath, true);
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
                Trace.WriteLine($"D={CmsPath.GetFileName(indexStoragePath)}: delete *.*", $"GenerationStorageStrategy.DeleteIndex");
                CmsDirectory.Delete(indexStoragePath, true);
            }
            catch (Exception ex)
            {
                eventLogService.LogError(nameof(GenerationStorageStrategy), nameof(DeleteIndex), ex.Message);
                return false;
            }

            return true;
        }
        finally
        {
            if (lockAcquired)
            {
                fileLock.Release();
            }
        }
    }

    public bool PerformCleanup(string indexStoragePath)
    {
        string toDeleteDir = CmsPath.Combine(indexStoragePath, IndexDeletionDirectoryName);
        if (!CmsDirectory.Exists(toDeleteDir))
        {
            return true;
        }

        try
        {
            var thrashDir = CmsDirectoryInfo.New(toDeleteDir);
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
                    CmsDirectory.Delete(dir.FullName, true);
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
            var dirInfo = CmsDirectoryInfo.New(directoryPath);
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
