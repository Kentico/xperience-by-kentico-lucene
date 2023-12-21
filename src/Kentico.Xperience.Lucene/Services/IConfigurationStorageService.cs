using Kentico.Xperience.Lucene.Models;

namespace Kentico.Xperience.Lucene.Services;

public interface IConfigurationStorageService
{
    Task<bool> TryEditIndex(LuceneConfigurationModel configuration);
    Task<bool> TryCreateIndex(LuceneConfigurationModel configuration);
    Task<bool> TryDeleteIndex(LuceneConfigurationModel configuration);
    Task<bool> TryDeleteIndex(int id);
    Task<LuceneConfigurationModel?> GetIndexDataOrNull(int indexId);
    Task<List<string>> GetExistingIndexNames();
    Task<IEnumerable<LuceneConfigurationModel>> GetAllIndexData();
}
