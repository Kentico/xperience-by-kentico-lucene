namespace Kentico.Xperience.Lucene.Core.Indexing;

public interface ILuceneConfigurationStorageService
{
    bool TryCreateIndex(LuceneIndexModel configuration);
    Task<bool> TryEditIndexAsync(LuceneIndexModel configuration);
    bool TryDeleteIndex(LuceneIndexModel configuration);
    bool TryDeleteIndex(int id);
    Task<LuceneIndexModel?> GetIndexDataOrNullAsync(int indexId);
    Task<LuceneIndexModel?> GetIndexDataOrNullAsync(string indexName);
    List<string> GetExistingIndexNames();
    List<int> GetIndexIds();
    Task<IEnumerable<LuceneIndexModel>> GetAllIndexDataAsync();
}
