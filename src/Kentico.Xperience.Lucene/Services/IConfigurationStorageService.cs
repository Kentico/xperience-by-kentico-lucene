using Kentico.Xperience.Lucene.Models;

namespace Kentico.Xperience.Lucene.Services;

public interface IConfigurationStorageService
{
    bool TryCreateIndex(LuceneConfigurationModel configuration);

    bool TryEditIndex(LuceneConfigurationModel configuration);
    bool TryDeleteIndex(LuceneConfigurationModel configuration);
    bool TryDeleteIndex(int id);
    LuceneConfigurationModel? GetIndexDataOrNull(int indexId);
    List<string> GetExistingIndexNames();
    List<int> GetIndexIds();
    IEnumerable<LuceneConfigurationModel> GetAllIndexData();
}
