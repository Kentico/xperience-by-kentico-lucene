using System.Text;

using CMS.Base;
using CMS.DataEngine;

namespace Kentico.Xperience.Lucene.Core.Indexing;

internal class DefaultLuceneConfigurationStorageService : ILuceneConfigurationStorageService
{
    private readonly IInfoProvider<LuceneIndexItemInfo> indexProvider;
    private readonly IInfoProvider<LuceneIncludedPathItemInfo> pathProvider;
    private readonly IInfoProvider<LuceneContentTypeItemInfo> contentTypeProvider;
    private readonly IInfoProvider<LuceneReusableContentTypeItemInfo> reusableContentTypeProvider;
    private readonly IInfoProvider<LuceneIndexLanguageItemInfo> languageProvider;

    public DefaultLuceneConfigurationStorageService(
        IInfoProvider<LuceneIndexItemInfo> indexProvider,
        IInfoProvider<LuceneIncludedPathItemInfo> pathProvider,
        IInfoProvider<LuceneContentTypeItemInfo> contentTypeProvider,
        IInfoProvider<LuceneReusableContentTypeItemInfo> reusableContentTypeProvider,
        IInfoProvider<LuceneIndexLanguageItemInfo> languageProvider
    )
    {
        this.indexProvider = indexProvider;
        this.pathProvider = pathProvider;
        this.contentTypeProvider = contentTypeProvider;
        this.languageProvider = languageProvider;
        this.reusableContentTypeProvider = reusableContentTypeProvider;
    }

    public bool TryCreateIndex(LuceneIndexModel configuration)
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
            LuceneIndexItemIndexName = configuration.IndexName ?? string.Empty,
            LuceneIndexItemChannelName = configuration.ChannelName ?? string.Empty,
            LuceneIndexItemStrategyName = configuration.StrategyName ?? string.Empty,
            LuceneIndexItemAnalyzerName = configuration.AnalyzerName ?? string.Empty,
            LuceneIndexItemRebuildHook = configuration.RebuildHook ?? string.Empty
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
                    foreach (var contentType in path.ContentTypes)
                    {
                        var contentInfo = new LuceneContentTypeItemInfo()
                        {
                            LuceneContentTypeItemContentTypeName = contentType.ContentTypeName,
                            LuceneContentTypeItemIncludedPathItemId = pathInfo.LuceneIncludedPathItemId,
                            LuceneContentTypeItemIndexItemId = newInfo.LuceneIndexItemId
                        };
                        contentInfo.Insert();
                    }
                }
            }
        }

        if (configuration.ReusableContentTypeNames is not null)
        {
            foreach (string? reusableContentTypeName in configuration.ReusableContentTypeNames)
            {
                var reusableContentTypeItemInfo = new LuceneReusableContentTypeItemInfo()
                {
                    LuceneReusableContentTypeItemContentTypeName = reusableContentTypeName,
                    LuceneReusableContentTypeItemIndexItemId = newInfo.LuceneIndexItemId
                };

                reusableContentTypeItemInfo.Insert();
            }
        }

        return true;
    }


    public async Task<LuceneIndexModel?> GetIndexDataOrNullAsync(int indexId)
    {
        var indexInfo = indexProvider.Get().WithID(indexId).FirstOrDefault();
        if (indexInfo == default)
        {
            return default;
        }

        var paths = pathProvider.Get().WhereEquals(nameof(LuceneIncludedPathItemInfo.LuceneIncludedPathItemIndexItemId), indexInfo.LuceneIndexItemId).GetEnumerableTypedResult();

        var webPageContentTypes = await GetLuceneWebPageContentTypesAsync();

        var languages = languageProvider.Get().WhereEquals(nameof(LuceneIndexLanguageItemInfo.LuceneIndexLanguageItemIndexItemId), indexInfo.LuceneIndexItemId).GetEnumerableTypedResult();

        var reusableContentTypes = reusableContentTypeProvider.Get().WhereEquals(nameof(LuceneReusableContentTypeItemInfo.LuceneReusableContentTypeItemIndexItemId), indexInfo.LuceneIndexItemId).GetEnumerableTypedResult();

        return new LuceneIndexModel(indexInfo, languages, paths, webPageContentTypes, reusableContentTypes);
    }


    public async Task<LuceneIndexModel?> GetIndexDataOrNullAsync(string indexName)
    {
        var indexInfo = indexProvider.Get().WhereEquals(nameof(LuceneIndexItemInfo.LuceneIndexItemIndexName), indexName).FirstOrDefault();
        if (indexInfo == default)
        {
            return default;
        }

        var paths = pathProvider.Get().WhereEquals(nameof(LuceneIncludedPathItemInfo.LuceneIncludedPathItemIndexItemId), indexInfo.LuceneIndexItemId).GetEnumerableTypedResult();

        var webPageContentTypes = await GetLuceneWebPageContentTypesAsync();

        var languages = languageProvider.Get().WhereEquals(nameof(LuceneIndexLanguageItemInfo.LuceneIndexLanguageItemIndexItemId), indexInfo.LuceneIndexItemId).GetEnumerableTypedResult();

        var reusableContentTypes = reusableContentTypeProvider.Get().WhereEquals(nameof(LuceneReusableContentTypeItemInfo.LuceneReusableContentTypeItemIndexItemId), indexInfo.LuceneIndexItemId).GetEnumerableTypedResult();

        return new LuceneIndexModel(indexInfo, languages, paths, webPageContentTypes, reusableContentTypes);
    }


    public List<string> GetExistingIndexNames() => indexProvider.Get().Select(x => x.LuceneIndexItemIndexName).ToList();


    public List<int> GetIndexIds() => indexProvider.Get().Select(x => x.LuceneIndexItemId).ToList();


    public async Task<IEnumerable<LuceneIndexModel>> GetAllIndexDataAsync()
    {
        var indexInfos = indexProvider.Get().GetEnumerableTypedResult().ToList();
        if (indexInfos.Count == 0)
        {
            return [];
        }

        var paths = pathProvider.Get().ToList();

        var webPageContentTypes = await GetLuceneWebPageContentTypesAsync();

        var languages = languageProvider.Get().ToList();

        var reusableContentTypes = reusableContentTypeProvider.Get().ToList();

        return indexInfos.Select(index => new LuceneIndexModel(index, languages, paths, webPageContentTypes, reusableContentTypes));
    }


    public async Task<bool> TryEditIndexAsync(LuceneIndexModel configuration)
    {
        configuration.IndexName = RemoveWhitespacesUsingStringBuilder(configuration.IndexName ?? string.Empty);

        var indexInfo = indexProvider.Get()
            .WhereEquals(nameof(LuceneIndexItemInfo.LuceneIndexItemId), configuration.Id)
            .TopN(1)
            .FirstOrDefault();

        if (indexInfo is null)
        {
            return false;
        }

        indexInfo.LuceneIndexItemRebuildHook = configuration.RebuildHook ?? string.Empty;
        indexInfo.LuceneIndexItemStrategyName = configuration.StrategyName ?? string.Empty;
        indexInfo.LuceneIndexItemAnalyzerName = configuration.AnalyzerName ?? string.Empty;
        indexInfo.LuceneIndexItemChannelName = configuration.ChannelName ?? string.Empty;
        indexInfo.LuceneIndexItemIndexName = configuration.IndexName ?? string.Empty;

        indexProvider.Set(indexInfo);

        RemoveUnusedIndexLanguages(configuration);
        await SetNewIndexLanguagesAsync(configuration, indexInfo);

        RemoveUnusedReusableContentTypes(configuration);
        await SetNewIndexReusableContentTypeItemsAsync(configuration, indexInfo);

        await RemoveUnusedIndexPathsAsync(configuration);
        var existingPaths = await GetExistingIndexPathsAsync(configuration);
        SetNewIndexPaths(configuration, existingPaths, indexInfo);
        UpdateEditedIndexPaths(configuration, existingPaths);

        var existingContentTypes = await GetExistingIndexContentTypesAsync(configuration);
        RemoveUnusedIndexContentTypesFromEditedPaths(existingContentTypes, configuration);
        SetNewIndexContentTypes(configuration, indexInfo, existingPaths, existingContentTypes);

        return true;
    }


    public bool TryDeleteIndex(int id)
    {
        indexProvider.BulkDelete(new WhereCondition($"{nameof(LuceneIndexItemInfo.LuceneIndexItemId)} = {id}"));
        pathProvider.BulkDelete(new WhereCondition($"{nameof(LuceneIncludedPathItemInfo.LuceneIncludedPathItemIndexItemId)} = {id}"));
        languageProvider.BulkDelete(new WhereCondition($"{nameof(LuceneIndexLanguageItemInfo.LuceneIndexLanguageItemIndexItemId)} = {id}"));
        contentTypeProvider.BulkDelete(new WhereCondition($"{nameof(LuceneContentTypeItemInfo.LuceneContentTypeItemIndexItemId)} = {id}"));
        reusableContentTypeProvider.BulkDelete(new WhereCondition($"{nameof(LuceneReusableContentTypeItemInfo.LuceneReusableContentTypeItemIndexItemId)} = {id}"));

        return true;
    }


    public bool TryDeleteIndex(LuceneIndexModel configuration)
    {
        indexProvider.BulkDelete(new WhereCondition($"{nameof(LuceneIndexItemInfo.LuceneIndexItemId)} = {configuration.Id}"));
        pathProvider.BulkDelete(new WhereCondition($"{nameof(LuceneIncludedPathItemInfo.LuceneIncludedPathItemIndexItemId)} = {configuration.Id}"));
        languageProvider.BulkDelete(new WhereCondition($"{nameof(LuceneIndexLanguageItemInfo.LuceneIndexLanguageItemIndexItemId)} = {configuration.Id}"));
        contentTypeProvider.BulkDelete(new WhereCondition($"{nameof(LuceneContentTypeItemInfo.LuceneContentTypeItemIndexItemId)} = {configuration.Id}"));
        reusableContentTypeProvider.BulkDelete(new WhereCondition($"{nameof(LuceneReusableContentTypeItemInfo.LuceneReusableContentTypeItemIndexItemId)} = {configuration.Id}"));

        return true;
    }


    private async Task<IEnumerable<LuceneIndexContentType>> GetLuceneWebPageContentTypesAsync()
        => await contentTypeProvider
            .Get().Source(x =>
                x.InnerJoin<DataClassInfo>(
                    nameof(LuceneContentTypeItemInfo.LuceneContentTypeItemContentTypeName),
                    nameof(DataClassInfo.ClassName))
            )
            .Columns(nameof(LuceneContentTypeItemInfo.LuceneContentTypeItemContentTypeName),
            nameof(DataClassInfo.ClassDisplayName),
            nameof(LuceneContentTypeItemInfo.LuceneContentTypeItemIncludedPathItemId))
            .GetEnumerableTypedResultAsync(x =>
            {
                var dataContainer = new DataRecordContainer(x);
                return new LuceneIndexContentType(
                    (string)dataContainer[nameof(LuceneContentTypeItemInfo.LuceneContentTypeItemContentTypeName)],
                    (string)dataContainer[nameof(DataClassInfo.ClassDisplayName)],
                    (int)dataContainer[nameof(LuceneContentTypeItemInfo.LuceneContentTypeItemIncludedPathItemId)]);
            });


    private void RemoveUnusedIndexLanguages(LuceneIndexModel configuration)
    {
        var removeLanguagesQuery = languageProvider
           .Get()
           .WhereEquals(nameof(LuceneIndexLanguageItemInfo.LuceneIndexLanguageItemIndexItemId), configuration.Id)
           .WhereNotIn(nameof(LuceneIndexLanguageItemInfo.LuceneIndexLanguageItemName), configuration.LanguageNames.ToArray());

        languageProvider.BulkDelete(new WhereCondition(removeLanguagesQuery));
    }


    private void RemoveUnusedReusableContentTypes(LuceneIndexModel configuration)
    {
        var removeReusableContentTypesQuery = reusableContentTypeProvider
            .Get()
            .WhereEquals(nameof(LuceneReusableContentTypeItemInfo.LuceneReusableContentTypeItemIndexItemId), configuration.Id)
            .WhereNotIn(nameof(LuceneReusableContentTypeItemInfo.LuceneReusableContentTypeItemContentTypeName), configuration.ReusableContentTypeNames.ToArray());

        reusableContentTypeProvider.BulkDelete(new WhereCondition(removeReusableContentTypesQuery));
    }


    private async Task<IEnumerable<string>> GetNewLanguagesOnIndexAsync(LuceneIndexModel configuration)
    {
        var existingLanguages = await languageProvider
             .Get()
             .WhereEquals(nameof(LuceneIndexLanguageItemInfo.LuceneIndexLanguageItemIndexItemId), configuration.Id)
             .GetEnumerableTypedResultAsync();

        return configuration.LanguageNames.Where(x => !existingLanguages.Any(y => y.LuceneIndexLanguageItemName == x));
    }


    private async Task<IEnumerable<string>> GetNewReusableContentTypesOnIndexAsync(LuceneIndexModel configuration)
    {
        var existingReusableContentTypes = await reusableContentTypeProvider
            .Get()
            .WhereEquals(nameof(LuceneReusableContentTypeItemInfo.LuceneReusableContentTypeItemIndexItemId), configuration.Id)
            .GetEnumerableTypedResultAsync();

        return configuration.ReusableContentTypeNames.Where(x => !existingReusableContentTypes.Any(y => y.LuceneReusableContentTypeItemContentTypeName == x));
    }


    private async Task SetNewIndexLanguagesAsync(LuceneIndexModel configuration, LuceneIndexItemInfo indexInfo)
    {
        var newLanguages = await GetNewLanguagesOnIndexAsync(configuration);

        foreach (string? language in newLanguages)
        {
            var languageInfo = new LuceneIndexLanguageItemInfo()
            {
                LuceneIndexLanguageItemName = language,
                LuceneIndexLanguageItemIndexItemId = indexInfo.LuceneIndexItemId,
            };

            languageProvider.Set(languageInfo);
        }
    }


    private async Task SetNewIndexReusableContentTypeItemsAsync(LuceneIndexModel configuration, LuceneIndexItemInfo indexInfo)
    {
        var newReusableContentTypes = await GetNewReusableContentTypesOnIndexAsync(configuration);

        foreach (string? reusableContentType in newReusableContentTypes)
        {
            var reusableContentTypeInfo = new LuceneReusableContentTypeItemInfo()
            {
                LuceneReusableContentTypeItemContentTypeName = reusableContentType,
                LuceneReusableContentTypeItemIndexItemId = indexInfo.LuceneIndexItemId,
            };

            reusableContentTypeProvider.Set(reusableContentTypeInfo);
        }
    }


    private async Task RemoveUnusedIndexPathsAsync(LuceneIndexModel configuration)
    {
        var removePathsQuery = pathProvider
            .Get()
            .WhereEquals(nameof(LuceneIncludedPathItemInfo.LuceneIncludedPathItemIndexItemId), configuration.Id)
            .WhereNotIn(nameof(LuceneIncludedPathItemInfo.LuceneIncludedPathItemId), configuration.Paths.Select(x => x.Identifier ?? 0).ToArray());

        var removedPaths = await removePathsQuery.GetEnumerableTypedResultAsync();

        pathProvider.BulkDelete(removePathsQuery);

        RemoveUnusedIndexContentTypes(removedPaths);
    }


    private async Task<IEnumerable<LuceneIncludedPathItemInfo>> GetExistingIndexPathsAsync(LuceneIndexModel configuration)
        => await pathProvider
            .Get()
            .WhereEquals(nameof(LuceneIncludedPathItemInfo.LuceneIncludedPathItemIndexItemId), configuration.Id)
            .GetEnumerableTypedResultAsync();


    private void SetNewIndexPaths(LuceneIndexModel configuration, IEnumerable<LuceneIncludedPathItemInfo> existingPaths, LuceneIndexItemInfo indexInfo)
    {
        var newPaths = configuration.Paths.Where(x => !existingPaths.Any(y => y.LuceneIncludedPathItemId == x.Identifier));

        foreach (var path in newPaths)
        {
            var pathInfo = new LuceneIncludedPathItemInfo()
            {
                LuceneIncludedPathItemAliasPath = path.AliasPath,
                LuceneIncludedPathItemIndexItemId = indexInfo.LuceneIndexItemId,
            };
            pathProvider.Set(pathInfo);

            if (path.ContentTypes != null)
            {
                foreach (var contentType in path.ContentTypes)
                {
                    var contentInfo = new LuceneContentTypeItemInfo()
                    {
                        LuceneContentTypeItemContentTypeName = contentType.ContentTypeName ?? string.Empty,
                        LuceneContentTypeItemIncludedPathItemId = pathInfo.LuceneIncludedPathItemId,
                        LuceneContentTypeItemIndexItemId = indexInfo.LuceneIndexItemId,
                    };
                    contentInfo.Insert();
                }
            }
        }
    }


    private void RemoveUnusedIndexContentTypes(IEnumerable<LuceneIncludedPathItemInfo> removedPaths)
    {
        var removeContentTypesQuery = contentTypeProvider
             .Get()
             .WhereIn(nameof(LuceneContentTypeItemInfo.LuceneContentTypeItemIncludedPathItemId), removedPaths.Select(x => x.LuceneIncludedPathItemId).ToArray());

        contentTypeProvider.BulkDelete(removeContentTypesQuery);
    }


    private async Task<IEnumerable<LuceneContentTypeItemInfo>> GetExistingIndexContentTypesAsync(LuceneIndexModel configuration)
        => await contentTypeProvider
        .Get()
        .WhereIn(nameof(LuceneContentTypeItemInfo.LuceneContentTypeItemIncludedPathItemId), configuration.Paths.Select(x => x.Identifier ?? 0).ToArray())
        .GetEnumerableTypedResultAsync();


    private void RemoveUnusedIndexContentTypesFromEditedPaths(IEnumerable<LuceneContentTypeItemInfo> allExistingContentTypes, LuceneIndexModel configuration)
    {
        int[] removedContentTypeIdsFromEditedPaths = allExistingContentTypes
                .Where(x => !configuration.Paths
                    .Any(y => y.ContentTypes
                        .Exists(z => x.LuceneContentTypeItemIncludedPathItemId == y.Identifier && x.LuceneContentTypeItemContentTypeName == z.ContentTypeName))
                )
                .Select(x => x.LuceneContentTypeItemId)
                .ToArray();

        contentTypeProvider.BulkDelete(contentTypeProvider.Get().WhereIn(nameof(LuceneContentTypeItemInfo.LuceneContentTypeItemId), removedContentTypeIdsFromEditedPaths));
    }


    private void UpdateEditedIndexPaths(LuceneIndexModel configuration, IEnumerable<LuceneIncludedPathItemInfo> existingPaths)
    {
        foreach (var path in existingPaths)
        {
            path.LuceneIncludedPathItemAliasPath = configuration.Paths.Single(x => x.Identifier == path.LuceneIncludedPathItemId).AliasPath;
            path.Update();
        }
    }


    private void SetNewIndexContentTypes(LuceneIndexModel configuration, LuceneIndexItemInfo indexInfo, IEnumerable<LuceneIncludedPathItemInfo> existingPaths, IEnumerable<LuceneContentTypeItemInfo> existingContentTypes)
    {
        foreach (var path in existingPaths)
        {
            foreach (var contentType in configuration.Paths
                .Single(x => x.Identifier == path.LuceneIncludedPathItemId)
                .ContentTypes
                .Where(x => !existingContentTypes
                    .Any(y => y.LuceneContentTypeItemContentTypeName == x.ContentTypeName && y.LuceneContentTypeItemIncludedPathItemId == path.LuceneIncludedPathItemId)
                )
            )
            {
                var contentInfo = new LuceneContentTypeItemInfo()
                {
                    LuceneContentTypeItemContentTypeName = contentType.ContentTypeName ?? string.Empty,
                    LuceneContentTypeItemIncludedPathItemId = path.LuceneIncludedPathItemId,
                    LuceneContentTypeItemIndexItemId = indexInfo.LuceneIndexItemId,
                };
                contentInfo.Insert();
            }
        }
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
}
