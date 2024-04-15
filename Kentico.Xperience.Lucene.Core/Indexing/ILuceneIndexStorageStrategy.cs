using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Kentico.Xperience.Lucene.Core.Indexing;

public interface ILuceneIndexStorageStrategy
{
    IEnumerable<IndexStorageModel> GetExistingIndices(string indexStoragePath);
    string FormatPath(string indexRoot, int generation, bool isPublished);
    string FormatTaxonomyPath(string indexRoot, int generation, bool isPublished);
    void PublishIndex(IndexStorageModel storage);
    bool ScheduleRemoval(IndexStorageModel storage);
    bool PerformCleanup(string indexStoragePath);
}

internal class GenerationStorageStrategy : ILuceneIndexStorageStrategy
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
