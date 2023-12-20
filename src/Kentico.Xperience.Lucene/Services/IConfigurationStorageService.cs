using Kentico.Xperience.Lucene.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kentico.Xperience.Lucene.Services
{
    public interface IConfigurationStorageService
    {
        Task<bool> TryEditIndex(LuceneConfigurationModel configuration);
        Task<bool> TryCreateIndex(LuceneConfigurationModel configuration);
        Task<bool> TryDeleteIndex(LuceneConfigurationModel configuration);
        Task<bool> TryDeleteIndex(int id);
        Task<LuceneConfigurationModel> GetIndexDataOrNull(int indexId);
        Task<List<string>> GetExistingIndexNames();
        Task<IEnumerable<LuceneConfigurationModel>> GetAllIndexData();
    }
}
