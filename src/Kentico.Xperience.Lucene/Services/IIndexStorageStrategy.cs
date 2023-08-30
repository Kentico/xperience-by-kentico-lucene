using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Kentico.Xperience.Lucene.Services;

public class IndexStorageContext
{
    private readonly IIndexStorageStrategy storageStrategy;
    private readonly string indexStoragePathRoot;

    public IndexStorageContext(IIndexStorageStrategy selectedStorageStrategy, string indexStoragePathRoot)
    {
        storageStrategy = selectedStorageStrategy;
        this.indexStoragePathRoot = indexStoragePathRoot;
    }

    public IndexStorageModel GetPublishedIndex() =>
        storageStrategy
            .GetExistingIndexes(indexStoragePathRoot)
            .Where(x => x.IsPublished)
            .MaxBy(x => x.Generation) ?? new IndexStorageModel(storageStrategy.FormatPath(indexStoragePathRoot, 1, true), 1, true);

    /// <summary>
    /// Gets next generation of index
    /// </summary>
    public IndexStorageModel GetNextGeneration()
    {
        var lastIndex = storageStrategy
            .GetExistingIndexes(indexStoragePathRoot)
            .MaxBy(x => x.Generation);

        var newIndex = lastIndex switch
        {
            var (path, generation, published) => new IndexStorageModel(path, published ? generation + 1 : generation, false),
            _ => new IndexStorageModel("", 1, false)
        };

        return newIndex with { Path = storageStrategy.FormatPath(indexStoragePathRoot, newIndex.Generation, newIndex.IsPublished) };
    }

    public IndexStorageModel GetLastGeneration(bool defaultPublished) =>
        storageStrategy
            .GetExistingIndexes(indexStoragePathRoot)
            .MaxBy(x => x.Generation)
        ?? new IndexStorageModel(storageStrategy.FormatPath(indexStoragePathRoot, 1, defaultPublished), 1, defaultPublished);

    /// <summary>
    /// method returns last writable index storage model
    /// </summary>
    /// <returns>Storage model with information about writable index</returns>
    /// <exception cref="ArgumentException">thrown when unexpected model occurs</exception>
    public IndexStorageModel GetNextOrOpenNextGeneration()
    {
        var lastIndex = storageStrategy
            .GetExistingIndexes(indexStoragePathRoot)
            .MaxBy(x => x.Generation);

        return lastIndex switch
        {
            { IsPublished: false } => lastIndex,
            (_, var generation, true) => new IndexStorageModel(storageStrategy.FormatPath(indexStoragePathRoot, generation + 1, false), generation + 1, false),
            null =>
                // no existing index, lets create new one
                new IndexStorageModel(storageStrategy.FormatPath(indexStoragePathRoot, 1, false), 1, false),
            _ => throw new ArgumentException($"Non-null last index storage with invalid settings '{lastIndex}'")
        };
    }

    public void PublishIndex(IndexStorageModel storage) => storageStrategy.PublishIndex(storage);
}

public record IndexStorageModel(string Path, int Generation, bool IsPublished);

public interface IIndexStorageStrategy
{
    IEnumerable<IndexStorageModel> GetExistingIndexes(string indexStoragePath);
    string FormatPath(string indexRoot, int generation, bool isPublished);
    void PublishIndex(IndexStorageModel storage);
}

public class GenerationStorageStrategy : IIndexStorageStrategy
{
    public IEnumerable<IndexStorageModel> GetExistingIndexes(string indexStoragePath)
    {
        if (Directory.Exists(indexStoragePath))
        {
            foreach (string directory in Directory.GetDirectories(indexStoragePath))
            {
                if (ParseIndexStorageModel(directory) is (true, var result))
                {
                    yield return result!;
                }
            }
        }
    }

    public string FormatPath(string indexRoot, int generation, bool isPublished) => Path.Combine(indexRoot, $"i-g{generation:0000000}-p_{isPublished}");

    public void PublishIndex(IndexStorageModel storage)
    {
        string root = Path.Combine(storage.Path, "..");
        var published = storage with { IsPublished = true, Path = FormatPath(root, storage.Generation, true) };
        Directory.Move(storage.Path, published.Path);
    }

    private record IndexStorageModelParsingResult(
        bool Success,
        [property: MemberNotNullWhen(true, "Success")] IndexStorageModel? Result
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
                var matchResult = Regex.Match(directoryName, "i-g(?<generation>[0-9]*)-p_(?<published>(true)|(false))", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                switch (matchResult)
                {
                    case { Success: true } r
                        when r.Groups["generation"] is { Success: true, Value: { Length: > 0 } gen } &&
                             r.Groups["published"] is { Success: true, Value: { Length: > 0 } pub }:
                    {
                        if (int.TryParse(gen, out int generation) && bool.TryParse(pub, out bool published))
                        {
                            return new IndexStorageModelParsingResult(true, new IndexStorageModel(directoryPath, generation, published));
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

public class SimpleStorageStrategy : IIndexStorageStrategy
{
    public IEnumerable<IndexStorageModel> GetExistingIndexes(string indexStoragePath) => new[] { new IndexStorageModel(indexStoragePath, 0, true) };
    public string FormatPath(string indexRoot, int generation, bool isPublished) => indexRoot;

    public void PublishIndex(IndexStorageModel storage)
    {
        // Method intentionally left empty. In this strategy, publication of index is not needed
    }
}
