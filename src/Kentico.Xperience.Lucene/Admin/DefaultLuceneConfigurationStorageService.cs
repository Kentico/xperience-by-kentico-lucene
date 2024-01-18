using System.Text;
using CMS.DataEngine;

namespace Kentico.Xperience.Lucene.Admin;

internal class DefaultLuceneConfigurationStorageService : ILuceneConfigurationStorageService
{
    private readonly ILuceneIndexItemInfoProvider indexProvider;
    private readonly ILuceneIncludedPathItemInfoProvider pathProvider;
    private readonly ILuceneContentTypeItemInfoProvider contentTypeProvider;
    private readonly ILuceneIndexLanguageItemInfoProvider languageProvider;

    public DefaultLuceneConfigurationStorageService(
        ILuceneIndexItemInfoProvider indexProvider,
        ILuceneIncludedPathItemInfoProvider pathProvider,
        ILuceneContentTypeItemInfoProvider contentTypeProvider,
        ILuceneIndexLanguageItemInfoProvider languageProvider
    )
    {
        this.indexProvider = indexProvider;
        this.pathProvider = pathProvider;
        this.contentTypeProvider = contentTypeProvider;
        this.languageProvider = languageProvider;
    }

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
        var existingIndex = indexProvider.Get()
            .WhereEquals(nameof(LuceneIndexItemInfo.LuceneIndexItemIndexName), configuration.IndexName)
            .TopN(1)
            .FirstOrDefault();

        if (existingIndex is not null)
        {
            return false;
        }

        var newInfo = new LuceneIndexItemInfo()
        {
            LuceneIndexItemIndexName = configuration.IndexName ?? "",
            LuceneIndexItemChannelName = configuration.ChannelName ?? "",
            LuceneIndexItemStrategyName = configuration.StrategyName ?? "",
            LuceneIndexItemRebuildHook = configuration.RebuildHook ?? ""
        };

        indexProvider.Set(newInfo);

        configuration.Id = newInfo.LuceneIndexItemId;

        if (configuration.LanguageNames is not null)
        {
            foreach (string? language in configuration.LanguageNames)
            {
                var languageInfo = new LuceneIndexLanguageItemInfo()
                {
                    LuceneIndexLanguageItemName = language,
                    LuceneIndexLanguageItemIndexItemId = newInfo.LuceneIndexItemId
                };

                languageInfo.Insert();
            }
        }

        if (configuration.Paths is not null)
        {
            foreach (var path in configuration.Paths)
            {
                var pathInfo = new LuceneIncludedPathItemInfo()
                {
                    LuceneIncludedPathItemAliasPath = path.AliasPath,
                    LuceneIncludedPathItemIndexItemId = newInfo.LuceneIndexItemId
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
                        contentInfo.Insert();
                    }
                }
            }
        }

        return true;
    }

    public LuceneConfigurationModel? GetIndexDataOrNull(int indexId)
    {
        var indexInfo = indexProvider.Get().WithID(indexId).FirstOrDefault();
        if (indexInfo == default)
        {
            return default;
        }

        var paths = pathProvider.Get().WhereEquals(nameof(LuceneIncludedPathItemInfo.LuceneIncludedPathItemIndexItemId), indexInfo.LuceneIndexItemId).GetEnumerableTypedResult();
        var contentTypes = contentTypeProvider.Get().WhereEquals(nameof(LuceneContentTypeItemInfo.LuceneContentTypeItemIndexItemId), indexInfo.LuceneIndexItemId).GetEnumerableTypedResult();
        var languages = languageProvider.Get().WhereEquals(nameof(LuceneIndexLanguageItemInfo.LuceneIndexLanguageItemIndexItemId), indexInfo.LuceneIndexItemId).GetEnumerableTypedResult();

        return new LuceneConfigurationModel(indexInfo, languages, paths, contentTypes);
    }

    public List<string> GetExistingIndexNames() => indexProvider.Get().Select(x => x.LuceneIndexItemIndexName).ToList();

    public List<int> GetIndexIds() => indexProvider.Get().Select(x => x.LuceneIndexItemId).ToList();

    public IEnumerable<LuceneConfigurationModel> GetAllIndexData()
    {
        var indexInfos = indexProvider.Get().ToList();
        if (indexInfos == default)
        {
            return new List<LuceneConfigurationModel>();
        }

        var paths = pathProvider.Get().ToList();
        var contentTypes = contentTypeProvider.Get().ToList();
        var languages = languageProvider.Get().ToList();

        return indexInfos.Select(index => new LuceneConfigurationModel(index, languages, paths, contentTypes));
    }

    public bool TryEditIndex(LuceneConfigurationModel configuration)
    {
        configuration.IndexName = RemoveWhitespacesUsingStringBuilder(configuration.IndexName ?? "");

        var indexInfo = indexProvider.Get()
            .WhereEquals(nameof(LuceneIndexItemInfo.LuceneIndexItemIndexName), configuration.IndexName)
            .TopN(1)
            .FirstOrDefault();

        if (indexInfo is null)
        {
            return false;
        }

        pathProvider.BulkDelete(new WhereCondition($"{nameof(LuceneIncludedPathItemInfo.LuceneIncludedPathItemIndexItemId)} = {configuration.Id}"));
        languageProvider.BulkDelete(new WhereCondition($"{nameof(LuceneIndexLanguageItemInfo.LuceneIndexLanguageItemIndexItemId)} = {configuration.Id}"));
        contentTypeProvider.BulkDelete(new WhereCondition($"{nameof(LuceneContentTypeItemInfo.LuceneContentTypeItemIndexItemId)} = {configuration.Id}"));

        indexInfo.LuceneIndexItemRebuildHook = configuration.RebuildHook ?? "";
        indexInfo.LuceneIndexItemStrategyName = configuration.StrategyName ?? "";
        indexInfo.LuceneIndexItemChannelName = configuration.ChannelName ?? "";

        indexProvider.Set(indexInfo);

        if (configuration.LanguageNames is not null)
        {
            foreach (string? language in configuration.LanguageNames)
            {
                var languageInfo = new LuceneIndexLanguageItemInfo()
                {
                    LuceneIndexLanguageItemName = language,
                    LuceneIndexLanguageItemIndexItemId = indexInfo.LuceneIndexItemId,
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
                    LuceneIncludedPathItemAliasPath = path.AliasPath,
                    LuceneIncludedPathItemIndexItemId = indexInfo.LuceneIndexItemId,
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
                            LuceneContentTypeItemIndexItemId = indexInfo.LuceneIndexItemId,
                        };
                        contentInfo.Insert();
                    }
                }
            }
        }

        return true;
    }

    public bool TryDeleteIndex(int id)
    {
        indexProvider.BulkDelete(new WhereCondition($"{nameof(LuceneIndexItemInfo.LuceneIndexItemId)} = {id}"));
        pathProvider.BulkDelete(new WhereCondition($"{nameof(LuceneIncludedPathItemInfo.LuceneIncludedPathItemIndexItemId)} = {id}"));
        languageProvider.BulkDelete(new WhereCondition($"{nameof(LuceneIndexLanguageItemInfo.LuceneIndexLanguageItemIndexItemId)} = {id}"));
        contentTypeProvider.BulkDelete(new WhereCondition($"{nameof(LuceneContentTypeItemInfo.LuceneContentTypeItemIndexItemId)} = {id}"));

        return true;
    }

    public bool TryDeleteIndex(LuceneConfigurationModel configuration)
    {
        indexProvider.BulkDelete(new WhereCondition($"{nameof(LuceneIndexItemInfo.LuceneIndexItemId)} = {configuration.Id}"));
        pathProvider.BulkDelete(new WhereCondition($"{nameof(LuceneIncludedPathItemInfo.LuceneIncludedPathItemIndexItemId)} = {configuration.Id}"));
        languageProvider.BulkDelete(new WhereCondition($"{nameof(LuceneIndexLanguageItemInfo.LuceneIndexLanguageItemIndexItemId)} = {configuration.Id}"));
        contentTypeProvider.BulkDelete(new WhereCondition($"{nameof(LuceneContentTypeItemInfo.LuceneContentTypeItemIndexItemId)} = {configuration.Id}"));

        return true;
    }
}
