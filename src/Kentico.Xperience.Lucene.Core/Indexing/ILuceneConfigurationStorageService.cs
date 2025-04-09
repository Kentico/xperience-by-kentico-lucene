namespace Kentico.Xperience.Lucene.Core.Indexing;

public interface ILuceneConfigurationStorageService
{
    public bool TryCreateIndex(LuceneIndexModel configuration);
    public Task<bool> TryEditIndexAsync(LuceneIndexModel configuration);
    public bool TryDeleteIndex(LuceneIndexModel configuration);
    public bool TryDeleteIndex(int id);
    public Task<LuceneIndexModel?> GetIndexDataOrNullAsync(int indexId);
    public Task<LuceneIndexModel?> GetIndexDataOrNullAsync(string indexName);
    public List<string> GetExistingIndexNames();
    public List<int> GetIndexIds();
    public Task<IEnumerable<LuceneIndexModel>> GetAllIndexDataAsync();
}
