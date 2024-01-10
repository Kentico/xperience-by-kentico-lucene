using System.Text;
using CMS.DataEngine;
using Kentico.Xperience.Lucene.Models;

namespace Kentico.Xperience.Lucene.Services;

public class DefaultConfigurationStorageService : IConfigurationStorageService
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
        var indexProvider = IndexItemInfoProvider.ProviderObject;
        var pathProvider = IncludedPathItemInfoProvider.ProviderObject;
        var contentPathProvider = ContentTypeItemInfoProvider.ProviderObject;
        var languageProvider = IndexedLanguageInfoProvider.ProviderObject;

        if (indexProvider.Get().WhereEquals(nameof(IndexItemInfo.IndexName), configuration.IndexName).FirstOrDefault() != default)
        {
            return false;
        }

        var newInfo = new IndexItemInfo()
        {
            IndexName = configuration.IndexName ?? "",
            ChannelName = configuration.ChannelName ?? "",
            StrategyName = configuration.StrategyName ?? ""
        };

        indexProvider.Set(newInfo);

        configuration.Id = newInfo.LuceneIndexItemId;

        if (configuration.LanguageNames is not null)
        {
            foreach (string? language in configuration.LanguageNames)
            {
                var languageInfo = new IndexedLanguageInfo()
                {
                    LanguageCode = language,
                    LuceneIndexItemId = newInfo.LuceneIndexItemId
                };

                languageProvider.Set(languageInfo);
            }
        }

        if (configuration.Paths is not null)
        {
            foreach (var path in configuration.Paths)
            {
                var pathInfo = new IncludedPathItemInfo()
                {
                    AliasPath = path.AliasPath,
                    LuceneIndexItemId = newInfo.LuceneIndexItemId
                };
                pathProvider.Set(pathInfo);

                if (path.ContentTypes is not null)
                {
                    foreach (string? contentType in path.ContentTypes)
                    {
                        var contentInfo = new ContentTypeItemInfo()
                        {
                            ContentTypeName = contentType,
                            LuceneIncludedPathItemId = pathInfo.LuceneIncludedPathItemId,
                            LuceneIndexItemId = newInfo.LuceneIndexItemId
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
        var pathProvider = IncludedPathItemInfoProvider.ProviderObject;
        var contentPathProvider = ContentTypeItemInfoProvider.ProviderObject;
        var indexProvider = IndexItemInfoProvider.ProviderObject;
        var languageProvider = IndexedLanguageInfoProvider.ProviderObject;


        var indexInfo = indexProvider.Get().WithID(indexId).FirstOrDefault();
        if (indexInfo == default)
        {
            return default;
        }

        var paths = pathProvider.Get().WhereEquals(nameof(IncludedPathItemInfo.LuceneIndexItemId), indexInfo.LuceneIndexItemId).ToList();
        var contentTypes = contentPathProvider.Get().WhereEquals(nameof(IncludedPathItemInfo.LuceneIndexItemId), indexInfo.LuceneIndexItemId).ToList();

        return new LuceneConfigurationModel()
        {
            ChannelName = indexInfo.ChannelName,
            IndexName = indexInfo.IndexName,
            LanguageNames = languageProvider.Get().WhereEquals(nameof(IndexedLanguageInfo.LuceneIndexItemId), indexInfo.LuceneIndexItemId).Select(x => x.LanguageCode).ToList(),
            RebuildHook = indexInfo.RebuildHook,
            Id = indexInfo.LuceneIndexItemId,
            StrategyName = indexInfo.StrategyName,
            Paths = paths.Select(x => new IncludedPath(x.AliasPath)
            {
                Identifier = x.LuceneIncludedPathItemId.ToString(),
                ContentTypes = contentTypes.Where(y => x.LuceneIncludedPathItemId == y.LuceneIncludedPathItemId).Select(y => y.ContentTypeName).ToArray()
            }).ToList()
        };
    }

    public List<string> GetExistingIndexNames() => IndexItemInfoProvider.ProviderObject.Get().Select(x => x.IndexName).ToList();

    public List<int> GetIndexIds() => IndexItemInfoProvider.ProviderObject.Get().Select(x => x.LuceneIndexItemId).ToList();

    public IEnumerable<LuceneConfigurationModel> GetAllIndexData()
    {
        var pathProvider = IncludedPathItemInfoProvider.ProviderObject;
        var contentPathProvider = ContentTypeItemInfoProvider.ProviderObject;
        var indexProvider = IndexItemInfoProvider.ProviderObject;
        var languageProvider = IndexedLanguageInfoProvider.ProviderObject;

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
            ChannelName = x.ChannelName,
            IndexName = x.IndexName,
            LanguageNames = languages.Where(y => y.LuceneIndexItemId == x.LuceneIndexItemId).Select(y => y.LanguageCode).ToList(),
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

    public bool TryEditIndex(LuceneConfigurationModel configuration)
    {
        var pathProvider = IncludedPathItemInfoProvider.ProviderObject;
        var contentPathProvider = ContentTypeItemInfoProvider.ProviderObject;
        var indexProvider = IndexItemInfoProvider.ProviderObject;
        var languageProvider = IndexedLanguageInfoProvider.ProviderObject;

        configuration.IndexName = RemoveWhitespacesUsingStringBuilder(configuration.IndexName ?? "");

        var indexInfo = indexProvider.Get().WhereEquals(nameof(IndexItemInfo.IndexName), configuration.IndexName).FirstOrDefault();

        if (indexInfo == default)
        {
            return false;
        }

        pathProvider.BulkDelete(new WhereCondition($"{nameof(IncludedPathItemInfo.LuceneIndexItemId)} = {configuration.Id}"));
        languageProvider.BulkDelete(new WhereCondition($"{nameof(IndexedLanguageInfo.LuceneIndexItemId)} = {configuration.Id}"));
        contentPathProvider.BulkDelete(new WhereCondition($"{nameof(ContentTypeItemInfo.LuceneIndexItemId)} = {configuration.Id}"));

        indexInfo.ChannelName = configuration.IndexName;
        indexInfo.StrategyName = configuration.StrategyName ?? "";
        indexInfo.ChannelName = configuration.ChannelName ?? "";

        indexProvider.Set(indexInfo);

        if (configuration.LanguageNames is not null)
        {
            foreach (string? language in configuration.LanguageNames)
            {
                var languageInfo = new IndexedLanguageInfo()
                {
                    LanguageCode = language,
                    LuceneIndexItemId = indexInfo.LuceneIndexItemId
                };

                languageProvider.Set(languageInfo);
            }
        }

        if (configuration.Paths is not null)
        {
            foreach (var path in configuration.Paths)
            {
                var pathInfo = new IncludedPathItemInfo()
                {
                    AliasPath = path.AliasPath,
                    LuceneIndexItemId = indexInfo.LuceneIndexItemId
                };
                pathProvider.Set(pathInfo);

                if (path.ContentTypes != null)
                {
                    foreach (string? contentType in path.ContentTypes)
                    {
                        var contentInfo = new ContentTypeItemInfo()
                        {
                            ContentTypeName = contentType ?? "",
                            LuceneIncludedPathItemId = pathInfo.LuceneIncludedPathItemId,
                            LuceneIndexItemId = indexInfo.LuceneIndexItemId
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
        var pathProvider = IncludedPathItemInfoProvider.ProviderObject;
        var contentPathProvider = ContentTypeItemInfoProvider.ProviderObject;
        var indexProvider = IndexItemInfoProvider.ProviderObject;
        var languageProvider = IndexedLanguageInfoProvider.ProviderObject;

        indexProvider.BulkDelete(new WhereCondition($"{nameof(IndexItemInfo.LuceneIndexItemId)} = {id}"));
        pathProvider.BulkDelete(new WhereCondition($"{nameof(IncludedPathItemInfo.LuceneIndexItemId)} = {id}"));
        languageProvider.BulkDelete(new WhereCondition($"{nameof(IndexedLanguageInfo.LuceneIndexItemId)} = {id}"));
        contentPathProvider.BulkDelete(new WhereCondition($"{nameof(ContentTypeItemInfo.LuceneIndexItemId)} = {id}"));
        return true;
    }

    public bool TryDeleteIndex(LuceneConfigurationModel configuration)
    {
        var pathProvider = IncludedPathItemInfoProvider.ProviderObject;
        var contentPathProvider = ContentTypeItemInfoProvider.ProviderObject;
        var indexProvider = IndexItemInfoProvider.ProviderObject;
        var languageProvider = IndexedLanguageInfoProvider.ProviderObject;

        indexProvider.BulkDelete(new WhereCondition($"{nameof(IndexItemInfo.LuceneIndexItemId)} = {configuration.Id}"));
        pathProvider.BulkDelete(new WhereCondition($"{nameof(IncludedPathItemInfo.LuceneIndexItemId)} = {configuration.Id}"));
        languageProvider.BulkDelete(new WhereCondition($"{nameof(IndexedLanguageInfo.LuceneIndexItemId)} = {configuration.Id}"));
        contentPathProvider.BulkDelete(new WhereCondition($"{nameof(ContentTypeItemInfo.LuceneIndexItemId)} = {configuration.Id}"));

        return true;
    }
}
