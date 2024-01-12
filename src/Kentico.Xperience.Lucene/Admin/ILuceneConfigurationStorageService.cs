namespace Kentico.Xperience.Lucene.Admin;

public interface ILuceneConfigurationStorageService
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
