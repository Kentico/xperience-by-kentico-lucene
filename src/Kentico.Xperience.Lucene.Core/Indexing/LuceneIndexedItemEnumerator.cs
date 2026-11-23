using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Helpers;
using CMS.Websites;

using Microsoft.Extensions.Options;

namespace Kentico.Xperience.Lucene.Core.Indexing;

/// <summary>
/// Enumerates the content items an index configuration selects (channels, paths, content types, languages).
/// Shared by index rebuilds and the admin diagnostics so both evaluate exactly the same set.
/// </summary>
internal class LuceneIndexedItemEnumerator
{
    private readonly IContentQueryExecutor executor;
    private readonly IInfoProvider<ContentLanguageInfo> languageProvider;
    private readonly IProgressiveCache cache;
    private readonly LuceneSearchOptions luceneSearchOptions;


    public LuceneIndexedItemEnumerator(
        IContentQueryExecutor executor,
        IInfoProvider<ContentLanguageInfo> languageProvider,
        IProgressiveCache cache,
        IOptions<LuceneSearchOptions> luceneSearchOptions)
    {
        this.executor = executor;
        this.languageProvider = languageProvider;
        this.cache = cache;
        this.luceneSearchOptions = luceneSearchOptions.Value;
    }


    public async Task<List<IIndexEventItemModel>> GetItemsAsync(LuceneIndex luceneIndex, CancellationToken cancellationToken)
    {
        var contentQueryExecutionOptions = new ContentQueryExecutionOptions
        {
            IncludeSecuredItems = luceneSearchOptions.IncludeSecuredItems
        };

        var indexedItems = new List<IIndexEventItemModel>();
        foreach (var channelConfiguration in luceneIndex.ChannelConfigurations)
        {
            foreach (var path in channelConfiguration.IncludedPaths)
            {
                var pathMatch =
                    LucenePathHelper.EndsWithWildcard(path.AliasPath)
                        ? PathMatch.Children(path.AliasPath[..^2])
                        : PathMatch.Single(path.AliasPath);

                foreach (string language in luceneIndex.LanguageNames)
                {
                    if (path.ContentTypes != null && path.ContentTypes.Count > 0)
                    {
                        var queryBuilder = new ContentItemQueryBuilder();

                        foreach (var contentType in path.ContentTypes)
                        {
                            queryBuilder.ForContentType(contentType.ContentTypeName,
                                config => config.ForWebsite(channelConfiguration.WebsiteChannelName, includeUrlPath: true, pathMatch: pathMatch));
                        }

                        queryBuilder.InLanguage(language);

                        var webpages = await executor.GetWebPageResult(queryBuilder,
                            container => container,
                            options: contentQueryExecutionOptions,
                            cancellationToken: cancellationToken);

                        foreach (var page in webpages)
                        {
                            var item = await MapToEventItem(page, channelConfiguration.WebsiteChannelName);
                            indexedItems.Add(item);
                        }
                    }
                }
            }
        }

        foreach (string language in luceneIndex.LanguageNames)
        {
            if (luceneIndex.IncludedReusableContentTypes != null && luceneIndex.IncludedReusableContentTypes.Count > 0)
            {
                var queryBuilder = new ContentItemQueryBuilder();

                foreach (string reusableContentType in luceneIndex.IncludedReusableContentTypes)
                {
                    queryBuilder.ForContentType(reusableContentType);
                }

                queryBuilder.InLanguage(language);

                var reusableItems = await executor.GetResult(queryBuilder,
                    result => result,
                    options: contentQueryExecutionOptions,
                    cancellationToken: cancellationToken);

                foreach (var reusableItem in reusableItems)
                {
                    var item = await MapToEventReusableItem(reusableItem);
                    indexedItems.Add(item);
                }
            }
        }

        return indexedItems;
    }


    private async Task<IndexEventWebPageItemModel> MapToEventItem(IWebPageContentQueryDataContainer content, string websiteChannelName)
    {
        var languages = await GetAllLanguages();

        string languageName = languages.FirstOrDefault(l => l.ContentLanguageID == content.ContentItemCommonDataContentLanguageID)?.ContentLanguageName ?? string.Empty;

        return new IndexEventWebPageItemModel(
            content.WebPageItemID,
            content.WebPageItemGUID,
            languageName,
            content.ContentTypeName,
            content.WebPageItemName,
            content.ContentItemIsSecured,
            content.ContentItemContentTypeID,
            content.ContentItemCommonDataContentLanguageID,
            websiteChannelName,
            content.WebPageItemTreePath,
            content.WebPageItemOrder);
    }


    private async Task<IndexEventReusableItemModel> MapToEventReusableItem(IContentQueryDataContainer content)
    {
        var languages = await GetAllLanguages();

        string languageName = languages.FirstOrDefault(l => l.ContentLanguageID == content.ContentItemCommonDataContentLanguageID)?.ContentLanguageName ?? string.Empty;

        return new IndexEventReusableItemModel(
            content.ContentItemID,
            content.ContentItemGUID,
            languageName,
            content.ContentTypeName,
            content.ContentItemName,
            content.ContentItemIsSecured,
            content.ContentItemContentTypeID,
            content.ContentItemCommonDataContentLanguageID);
    }


    private Task<IEnumerable<ContentLanguageInfo>> GetAllLanguages() =>
        cache.LoadAsync(async cs =>
        {
            var results = await languageProvider.Get()
                .GetEnumerableTypedResultAsync(cancellationToken: CancellationToken.None);

            cs.GetCacheDependency = () => CacheHelper.GetCacheDependency($"{ContentLanguageInfo.OBJECT_TYPE}|all");

            return results;
        }, new CacheSettings(5, nameof(LuceneIndexedItemEnumerator), nameof(GetAllLanguages)));
}
