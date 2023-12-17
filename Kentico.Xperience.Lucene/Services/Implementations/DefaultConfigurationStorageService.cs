using Kentico.Xperience.Lucene.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using CMS;
using CMS.DataEngine;
using System.Text;
using System.Security.Cryptography.Xml;

namespace Kentico.Xperience.Lucene.Services.Implementations
{
    public class DefaultConfigurationStorageService : IConfigurationStorageService
    {
        private static string RemoveWhitespacesUsingStringBuilder(string source)
        {
            var builder = new StringBuilder(source.Length);
            for (int i = 0; i < source.Length; i++)
            {
                char c = source[i];
                if (!char.IsWhiteSpace(c))
                    builder.Append(c);
            }
            return source.Length == builder.Length ? source : builder.ToString();
        }
        public async Task<bool> TryCreateIndex(LuceneConfigurationModel configuration)
        {
            var indexProvider = IndexitemInfoProvider.ProviderObject;
            var pathProvider = IncludedpathitemInfoProvider.ProviderObject;
            var contentPathProvider = ContenttypeitemInfoProvider.ProviderObject;
            var languageProvider = IndexedlanguageInfoProvider.ProviderObject;

            if (indexProvider.Get().WhereEquals(nameof(IndexitemInfo.IndexName), configuration.IndexName).FirstOrDefault() != default)
            {
                return false;
            }

            var newInfo = new IndexitemInfo()
            {
                IndexName = configuration.IndexName,
                ChannelName = configuration.ChannelName,
                StrategyName = configuration.StrategyName
            };

            indexProvider.Set(newInfo);

            configuration.Id = newInfo.LuceneIndexItemId;

            foreach (var language in configuration.LanguageNames)
            {
                var languageInfo = new IndexedlanguageInfo()
                {
                    languageCode = language,
                    LuceneIndexItemId = newInfo.LuceneIndexItemId
                };

                languageProvider.Set(languageInfo);
            }

            foreach (var path in configuration.Paths)
            {
                var pathInfo = new IncludedpathitemInfo()
                {
                    AliasPath = path.AliasPath,
                    LuceneIndexItemId = newInfo.LuceneIndexItemId
                };
                pathProvider.Set(pathInfo);

                foreach (var contentType in path.ContentTypes)
                {
                    var contentInfo = new ContenttypeitemInfo()
                    {
                        ContentTypeName = contentType,
                        LuceneIncludedPathItemId = pathInfo.LuceneIncludedPathItemId,
                        LuceneIndexItemId = newInfo.LuceneIndexItemId
                    };
                    contentPathProvider.Set(contentInfo);
                }
            }

            return true;
        }

        public async Task<LuceneConfigurationModel> GetIndexDataOrNull(int indexId)
        {
            var pathProvider = IncludedpathitemInfoProvider.ProviderObject;
            var contentPathProvider = ContenttypeitemInfoProvider.ProviderObject;
            var indexProvider = IndexitemInfoProvider.ProviderObject;
            var languageProvider = IndexedlanguageInfoProvider.ProviderObject;

            var indexInfo = indexProvider.Get().WithID(indexId).FirstOrDefault();
            if (indexInfo == default)
            {
                return null;
            }

            var paths = pathProvider.Get().WhereEquals(nameof(IncludedpathitemInfo.LuceneIndexItemId), indexInfo.LuceneIndexItemId).ToList();
            var contentTypes = contentPathProvider.Get().WhereEquals(nameof(IncludedpathitemInfo.LuceneIndexItemId), indexInfo.LuceneIndexItemId).ToList();

            var index = new LuceneConfigurationModel()
            {
                ChannelName = indexInfo.ChannelName,
                IndexName = indexInfo.IndexName,
                LanguageNames = languageProvider.Get().WhereEquals(nameof(IndexedlanguageInfo.LuceneIndexItemId), indexInfo.LuceneIndexItemId).Select(x => x.languageCode).ToList(),
                RebuildHook = indexInfo.RebuildHook,
                Id = indexInfo.LuceneIndexItemId,
                StrategyName = indexInfo.StrategyName,
                Paths = paths.Select(x => new IncludedPath(x.AliasPath)
                { 
                    Identifier = x.LuceneIncludedPathItemId.ToString(),
                    ContentTypes = contentTypes.Where(y => x.LuceneIncludedPathItemId == y.LuceneIncludedPathItemId).Select(y => y.ContentTypeName).ToArray()
                }).ToList()
            };

            return index;
        }

        public async Task<List<string>> GetExistingIndexNames()
        {
            return IndexitemInfoProvider.ProviderObject.Get().Select(x => x.IndexName).ToList();
        }

        public async Task<IEnumerable<LuceneConfigurationModel>> GetAllIndexData()
        {
            var pathProvider = IncludedpathitemInfoProvider.ProviderObject;
            var contentPathProvider = ContenttypeitemInfoProvider.ProviderObject;
            var indexProvider = IndexitemInfoProvider.ProviderObject;
            var languageProvider = IndexedlanguageInfoProvider.ProviderObject;

            var indexInfos = indexProvider.Get().ToList();
            if (indexInfos == default)
            {
                return null;
            }

            var paths = pathProvider.Get().ToList();
            var contentTypes = contentPathProvider.Get().ToList();
            var languages = languageProvider.Get().ToList();

            return indexInfos.Select(x => new LuceneConfigurationModel
            {
                ChannelName = x.ChannelName,
                IndexName = x.IndexName,
                LanguageNames = languages.Where(y => y.LuceneIndexItemId == x.LuceneIndexItemId).Select(y => y.languageCode).ToList(),
                RebuildHook = x.RebuildHook,
                Id = x.LuceneIndexItemId,
                StrategyName = x.StrategyName,
                Paths = paths.Where(y => y.LuceneIndexItemId == x.LuceneIndexItemId).Select(y => new IncludedPath(y.AliasPath)
                { 
                    Identifier = y.LuceneIncludedPathItemId.ToString(),
                    ContentTypes = contentTypes.Where(z => z.LuceneIncludedPathItemId == y.LuceneIncludedPathItemId).Select(z => z.ContentTypeName).ToArray()
                }).ToList()
            });
        }

        public async Task<bool> TryEditIndex(LuceneConfigurationModel configuration)
        {
            var pathProvider = IncludedpathitemInfoProvider.ProviderObject;
            var contentPathProvider = ContenttypeitemInfoProvider.ProviderObject;
            var indexProvider = IndexitemInfoProvider.ProviderObject;
            var languageProvider = IndexedlanguageInfoProvider.ProviderObject;

            configuration.IndexName = RemoveWhitespacesUsingStringBuilder(configuration.IndexName);

            var indexInfo = indexProvider.Get().WhereEquals(nameof(IndexitemInfo.IndexName),configuration.IndexName).FirstOrDefault();

            if (indexInfo == default)
            {
                return false;
            }

            pathProvider.BulkDelete(new WhereCondition($"{nameof(IncludedpathitemInfo.LuceneIndexItemId)} = {configuration.Id}"));
            languageProvider.BulkDelete(new WhereCondition($"{nameof(IndexedlanguageInfo.LuceneIndexItemId)} = {configuration.Id}"));
            contentPathProvider.BulkDelete(new WhereCondition($"{nameof(ContenttypeitemInfo.LuceneIndexItemId)} = {configuration.Id}"));

            indexInfo.ChannelName = configuration.IndexName;
            indexInfo.StrategyName = configuration.StrategyName;
            indexInfo.ChannelName = configuration.ChannelName;

            indexProvider.Set(indexInfo);

            foreach (var language in configuration.LanguageNames)
            {
                var languageInfo = new IndexedlanguageInfo()
                {
                    languageCode = language,
                    LuceneIndexItemId = indexInfo.LuceneIndexItemId
                };

                languageProvider.Set(languageInfo);
            }

            foreach (var path in configuration.Paths)
            {
                var pathInfo = new IncludedpathitemInfo()
                {
                    AliasPath = path.AliasPath,
                    LuceneIndexItemId = indexInfo.LuceneIndexItemId
                };
                pathProvider.Set(pathInfo);

                foreach (var contentType in path.ContentTypes)
                {
                    var contentInfo = new ContenttypeitemInfo()
                    {
                        ContentTypeName = contentType,
                        LuceneIncludedPathItemId = pathInfo.LuceneIncludedPathItemId,
                        LuceneIndexItemId = indexInfo.LuceneIndexItemId
                    };
                    contentPathProvider.Set(contentInfo);
                }
            }

            return true;
        }

        public async Task<bool> TryDeleteIndex(int id)
        {
            var pathProvider = IncludedpathitemInfoProvider.ProviderObject;
            var contentPathProvider = ContenttypeitemInfoProvider.ProviderObject;
            var indexProvider = IndexitemInfoProvider.ProviderObject;
            var languageProvider = IndexedlanguageInfoProvider.ProviderObject;

            indexProvider.BulkDelete(new WhereCondition($"{nameof(IndexitemInfo.LuceneIndexItemId)} = {id}"));
            pathProvider.BulkDelete(new WhereCondition($"{nameof(IncludedpathitemInfo.LuceneIndexItemId)} = {id}"));
            languageProvider.BulkDelete(new WhereCondition($"{nameof(IndexedlanguageInfo.LuceneIndexItemId)} = {id}"));
            contentPathProvider.BulkDelete(new WhereCondition($"{nameof(ContenttypeitemInfo.LuceneIndexItemId)} = {id}"));

            return true;
        }

        public async Task<bool> TryDeleteIndex(LuceneConfigurationModel configuration)
        {
            var pathProvider = IncludedpathitemInfoProvider.ProviderObject;
            var contentPathProvider = ContenttypeitemInfoProvider.ProviderObject;
            var indexProvider = IndexitemInfoProvider.ProviderObject;
            var languageProvider = IndexedlanguageInfoProvider.ProviderObject;
            
            indexProvider.BulkDelete(new WhereCondition($"{nameof(IndexitemInfo.LuceneIndexItemId)} = {configuration.Id}"));
            pathProvider.BulkDelete(new WhereCondition($"{nameof(IncludedpathitemInfo.LuceneIndexItemId)} = {configuration.Id}"));
            languageProvider.BulkDelete(new WhereCondition($"{nameof(IndexedlanguageInfo.LuceneIndexItemId)} = {configuration.Id}"));
            contentPathProvider.BulkDelete(new WhereCondition($"{nameof(ContenttypeitemInfo.LuceneIndexItemId)} = {configuration.Id}"));
            
            return true;
        }
    }
}
