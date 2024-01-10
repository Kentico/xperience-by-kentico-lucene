using System.Text;
using CMS.DataEngine;
using Kentico.Xperience.Lucene.Models;

namespace Kentico.Xperience.Lucene.Admin;

public class DefaultLuceneConfigurationStorageService : ILuceneConfigurationStorageService
{
    private static string RemoveWhitespacesUsingStringBuilder(string source)
    {
        var builder = new StringBuilder(source.Length);
        for (int i = 0; i < source.Length; i++)
        {
            char c = source[i];
            if (!char.IsWhiteSpace(c))
            {
                builder.Append(c);
            }
        }
        return source.Length == builder.Length ? source : builder.ToString();
    }
    public bool TryCreateIndex(LuceneConfigurationModel configuration)
    {
        var indexProvider = LuceneIndexItemInfoProvider.ProviderObject;
        var pathProvider = LuceneIncludedPathItemInfoProvider.ProviderObject;
        var contentPathProvider = LuceneContentTypeItemInfoProvider.ProviderObject;
        var languageProvider = LuceneIndexedLanguageInfoProvider.ProviderObject;

        if (indexProvider.Get().WhereEquals(nameof(LuceneIndexItemInfo.LuceneIndexItemIndexName), configuration.IndexName).FirstOrDefault() != default)
        {
            return false;
        }

        var newInfo = new LuceneIndexItemInfo()
        {
            LuceneIndexItemIndexName = configuration.IndexName ?? "",
            LuceneIndexItemChannelName = configuration.ChannelName ?? "",
            LuceneIndexItemStrategyName = configuration.StrategyName ?? ""
        };

        indexProvider.Set(newInfo);

        configuration.Id = newInfo.LuceneIndexItemId;

        if (configuration.LanguageNames is not null)
        {
            foreach (string? language in configuration.LanguageNames)
            {
                var languageInfo = new LuceneIndexedLanguageInfo()
                {
                    LuceneIndexedLanguageName = language,
                    LuceneIndexedLanguageIndexItemId = newInfo.LuceneIndexItemId
                };

                languageProvider.Set(languageInfo);
            }
        }

        if (configuration.Paths is not null)
        {
            foreach (var path in configuration.Paths)
            {
                var pathInfo = new LuceneIncludedPathItemInfo()
                {
                    LuceneIncludedPathAliasPath = path.AliasPath,
                    LuceneIncludedPathIndexItemId = newInfo.LuceneIndexItemId
                };
                pathProvider.Set(pathInfo);

                if (path.ContentTypes is not null)
                {
                    foreach (string? contentType in path.ContentTypes)
                    {
                        var contentInfo = new LuceneContentTypeItemInfo()
                        {
                            LuceneContentTypeItemContentTypeName = contentType,
                            LuceneContentTypeItemIncludedPathItemId = pathInfo.LuceneIncludedPathItemId,
                            LuceneContentTypeItemIndexItemId = newInfo.LuceneIndexItemId
                        };
                        contentPathProvider.Set(contentInfo);
                    }
                }
            }
        }

        return true;
    }

    public LuceneConfigurationModel? GetIndexDataOrNull(int indexId)
    {
        var pathProvider = LuceneIncludedPathItemInfoProvider.ProviderObject;
        var contentPathProvider = LuceneContentTypeItemInfoProvider.ProviderObject;
        var indexProvider = LuceneIndexItemInfoProvider.ProviderObject;
        var languageProvider = LuceneIndexedLanguageInfoProvider.ProviderObject;


        var indexInfo = indexProvider.Get().WithID(indexId).FirstOrDefault();
        if (indexInfo == default)
        {
            return default;
        }

        var paths = pathProvider.Get().WhereEquals(nameof(LuceneIncludedPathItemInfo.LuceneIncludedPathIndexItemId), indexInfo.LuceneIndexItemId).ToList();
        var contentTypes = contentPathProvider.Get().WhereEquals(nameof(LuceneIncludedPathItemInfo.LuceneIncludedPathIndexItemId), indexInfo.LuceneIndexItemId).ToList();

        return new LuceneConfigurationModel()
        {
            ChannelName = indexInfo.LuceneIndexItemChannelName,
            IndexName = indexInfo.LuceneIndexItemIndexName,
            LanguageNames = languageProvider.Get().WhereEquals(nameof(LuceneIndexedLanguageInfo.LuceneIndexedLanguageIndexItemId), indexInfo.LuceneIndexItemId).Select(x => x.LuceneIndexedLanguageName).ToList(),
            RebuildHook = indexInfo.LuceneIndexItemRebuildHook,
            Id = indexInfo.LuceneIndexItemId,
            StrategyName = indexInfo.LuceneIndexItemStrategyName,
            Paths = paths.Select(x => new LuceneIndexIncludedPath(x.LuceneIncludedPathAliasPath)
            {
                Identifier = x.LuceneIncludedPathItemId.ToString(),
                ContentTypes = contentTypes.Where(y => x.LuceneIncludedPathItemId == y.LuceneContentTypeItemIncludedPathItemId).Select(y => y.LuceneContentTypeItemContentTypeName).ToArray()
            }).ToList()
        };
    }

    public List<string> GetExistingIndexNames() => LuceneIndexItemInfoProvider.ProviderObject.Get().Select(x => x.LuceneIndexItemIndexName).ToList();

    public List<int> GetIndexIds() => LuceneIndexItemInfoProvider.ProviderObject.Get().Select(x => x.LuceneIndexItemId).ToList();

    public IEnumerable<LuceneConfigurationModel> GetAllIndexData()
    {
        var pathProvider = LuceneIncludedPathItemInfoProvider.ProviderObject;
        var contentPathProvider = LuceneContentTypeItemInfoProvider.ProviderObject;
        var indexProvider = LuceneIndexItemInfoProvider.ProviderObject;
        var languageProvider = LuceneIndexedLanguageInfoProvider.ProviderObject;

        var indexInfos = indexProvider.Get().ToList();
        if (indexInfos == default)
        {
            return new List<LuceneConfigurationModel>();
        }

        var paths = pathProvider.Get().ToList();
        var contentTypes = contentPathProvider.Get().ToList();
        var languages = languageProvider.Get().ToList();

        return indexInfos.Select(x => new LuceneConfigurationModel
        {
            ChannelName = x.LuceneIndexItemChannelName,
            IndexName = x.LuceneIndexItemIndexName,
            LanguageNames = languages.Where(y => y.LuceneIndexedLanguageIndexItemId == x.LuceneIndexItemId).Select(y => y.LuceneIndexedLanguageName).ToList(),
            RebuildHook = x.LuceneIndexItemRebuildHook,
            Id = x.LuceneIndexItemId,
            StrategyName = x.LuceneIndexItemStrategyName,
            Paths = paths.Where(y => y.LuceneIncludedPathIndexItemId == x.LuceneIndexItemId).Select(y => new LuceneIndexIncludedPath(y.LuceneIncludedPathAliasPath)
            {
                Identifier = y.LuceneIncludedPathItemId.ToString(),
                ContentTypes = contentTypes.Where(z => z.LuceneContentTypeItemIncludedPathItemId == y.LuceneIncludedPathItemId).Select(z => z.LuceneContentTypeItemContentTypeName).ToArray()
            }).ToList()
        });
    }

    public bool TryEditIndex(LuceneConfigurationModel configuration)
    {
        var pathProvider = LuceneIncludedPathItemInfoProvider.ProviderObject;
        var contentPathProvider = LuceneContentTypeItemInfoProvider.ProviderObject;
        var indexProvider = LuceneIndexItemInfoProvider.ProviderObject;
        var languageProvider = LuceneIndexedLanguageInfoProvider.ProviderObject;

        configuration.IndexName = RemoveWhitespacesUsingStringBuilder(configuration.IndexName ?? "");

        var indexInfo = indexProvider.Get().WhereEquals(nameof(LuceneIndexItemInfo.LuceneIndexItemIndexName), configuration.IndexName).FirstOrDefault();

        if (indexInfo == default)
        {
            return false;
        }

        pathProvider.BulkDelete(new WhereCondition($"{nameof(LuceneIncludedPathItemInfo.LuceneIncludedPathIndexItemId)} = {configuration.Id}"));
        languageProvider.BulkDelete(new WhereCondition($"{nameof(LuceneIndexedLanguageInfo.LuceneIndexedLanguageIndexItemId)} = {configuration.Id}"));
        contentPathProvider.BulkDelete(new WhereCondition($"{nameof(LuceneContentTypeItemInfo.LuceneContentTypeItemIndexItemId)} = {configuration.Id}"));

        indexInfo.LuceneIndexItemChannelName = configuration.IndexName;
        indexInfo.LuceneIndexItemStrategyName = configuration.StrategyName ?? "";
        indexInfo.LuceneIndexItemChannelName = configuration.ChannelName ?? "";

        indexProvider.Set(indexInfo);

        if (configuration.LanguageNames is not null)
        {
            foreach (string? language in configuration.LanguageNames)
            {
                var languageInfo = new LuceneIndexedLanguageInfo()
                {
                    LuceneIndexedLanguageName = language,
                    LuceneIndexedLanguageIndexItemId = indexInfo.LuceneIndexItemId
                };

                languageProvider.Set(languageInfo);
            }
        }

        if (configuration.Paths is not null)
        {
            foreach (var path in configuration.Paths)
            {
                var pathInfo = new LuceneIncludedPathItemInfo()
                {
                    LuceneIncludedPathAliasPath = path.AliasPath,
                    LuceneIncludedPathIndexItemId = indexInfo.LuceneIndexItemId
                };
                pathProvider.Set(pathInfo);

                if (path.ContentTypes != null)
                {
                    foreach (string? contentType in path.ContentTypes)
                    {
                        var contentInfo = new LuceneContentTypeItemInfo()
                        {
                            LuceneContentTypeItemContentTypeName = contentType ?? "",
                            LuceneContentTypeItemIncludedPathItemId = pathInfo.LuceneIncludedPathItemId,
                            LuceneContentTypeItemIndexItemId = indexInfo.LuceneIndexItemId
                        };
                        contentPathProvider.Set(contentInfo);
                    }
                }
            }
        }

        return true;
    }

    public bool TryDeleteIndex(int id)
    {
        var pathProvider = LuceneIncludedPathItemInfoProvider.ProviderObject;
        var contentPathProvider = LuceneContentTypeItemInfoProvider.ProviderObject;
        var indexProvider = LuceneIndexItemInfoProvider.ProviderObject;
        var languageProvider = LuceneIndexedLanguageInfoProvider.ProviderObject;

        indexProvider.BulkDelete(new WhereCondition($"{nameof(LuceneIndexItemInfo.LuceneIndexItemId)} = {id}"));
        pathProvider.BulkDelete(new WhereCondition($"{nameof(LuceneIncludedPathItemInfo.LuceneIncludedPathIndexItemId)} = {id}"));
        languageProvider.BulkDelete(new WhereCondition($"{nameof(LuceneIndexedLanguageInfo.LuceneIndexedLanguageIndexItemId)} = {id}"));
        contentPathProvider.BulkDelete(new WhereCondition($"{nameof(LuceneContentTypeItemInfo.LuceneContentTypeItemIndexItemId)} = {id}"));
        return true;
    }

    public bool TryDeleteIndex(LuceneConfigurationModel configuration)
    {
        var pathProvider = LuceneIncludedPathItemInfoProvider.ProviderObject;
        var contentPathProvider = LuceneContentTypeItemInfoProvider.ProviderObject;
        var indexProvider = LuceneIndexItemInfoProvider.ProviderObject;
        var languageProvider = LuceneIndexedLanguageInfoProvider.ProviderObject;

        indexProvider.BulkDelete(new WhereCondition($"{nameof(LuceneIndexItemInfo.LuceneIndexItemId)} = {configuration.Id}"));
        pathProvider.BulkDelete(new WhereCondition($"{nameof(LuceneIncludedPathItemInfo.LuceneIncludedPathIndexItemId)} = {configuration.Id}"));
        languageProvider.BulkDelete(new WhereCondition($"{nameof(LuceneIndexedLanguageInfo.LuceneIndexedLanguageIndexItemId)} = {configuration.Id}"));
        contentPathProvider.BulkDelete(new WhereCondition($"{nameof(LuceneContentTypeItemInfo.LuceneContentTypeItemIndexItemId)} = {configuration.Id}"));

        return true;
    }
}
