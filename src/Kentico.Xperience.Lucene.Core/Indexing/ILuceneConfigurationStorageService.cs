namespace Kentico.Xperience.Lucene.Core.Indexing;

public interface ILuceneConfigurationStorageService
{
    bool TryCreateIndex(LuceneIndexModel configuration);
    Task<bool> TryEditIndexAsync(LuceneIndexModel configuration);
    bool TryDeleteIndex(LuceneIndexModel configuration);
    bool TryDeleteIndex(int id);
    LuceneIndexModel? GetIndexDataOrNull(int indexId);
    LuceneIndexModel? GetIndexDataOrNull(string indexName);
    List<string> GetExistingIndexNames();
    List<int> GetIndexIds();
    IEnumerable<LuceneIndexModel> GetAllIndexData();
}
