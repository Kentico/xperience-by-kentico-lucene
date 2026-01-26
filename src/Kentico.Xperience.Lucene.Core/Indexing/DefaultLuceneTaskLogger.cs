using CMS.ContentEngine;
using CMS.ContentEngine.Internal;
using CMS.Core;
using CMS.DataEngine;
using CMS.Websites;
using CMS.Websites.Internal;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Kentico.Xperience.Lucene.Core.Indexing;

/// <summary>
/// Default implementation of <see cref="ILuceneTaskLogger"/>.
/// </summary>
internal class DefaultLuceneTaskLogger : ILuceneTaskLogger
{
    private readonly IEventLogService eventLogService;
    private readonly IServiceProvider serviceProvider;
    private readonly ILuceneIndexManager indexManager;
    private readonly IInfoProvider<ContentLanguageInfo> contentLanguageInfoProvider;
    private readonly IInfoProvider<WebPageItemInfo> webPageItemInfoProvider;
    private readonly LuceneSearchOptions luceneSearchOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultLuceneTaskLogger"/> class.
    /// </summary>
    public DefaultLuceneTaskLogger(IEventLogService eventLogService,
        IServiceProvider serviceProvider,
        ILuceneIndexManager indexManager,
        IInfoProvider<ContentLanguageInfo> contentLanguageInfoProvider,
        IInfoProvider<WebPageItemInfo> webPageItemInfoProvider,
        IOptions<LuceneSearchOptions> luceneSearchOptions)
    {
        this.eventLogService = eventLogService;
        this.serviceProvider = serviceProvider;
        this.indexManager = indexManager;
        this.contentLanguageInfoProvider = contentLanguageInfoProvider;
        this.webPageItemInfoProvider = webPageItemInfoProvider;
        this.luceneSearchOptions = luceneSearchOptions.Value;
    }

    /// <inheritdoc />
    public async Task HandleEvent(IndexEventWebPageItemModel webpageItem, string eventName)
    {
        var taskType = GetTaskType(eventName);

        await HandleEventInternal(webpageItem, eventName, taskType);
    }

    /// <inheritdoc />
    public async Task HandleReusableItemEvent(IndexEventReusableItemModel reusableItem, string eventName)
    {
        var taskType = GetTaskType(eventName);

        foreach (var luceneIndex in indexManager.GetAllIndices())
        {
            if (!reusableItem.IsIndexedByIndex(eventLogService, indexManager, luceneIndex.IndexName, eventName))
            {
                continue;
            }

            IEnumerable<IIndexEventItemModel>? toReindex = null;

            // For delete events, use pre-captured RelatedItems if available to avoid querying deleted items
            if (taskType == LuceneTaskType.DELETE && reusableItem.RelatedItems.Any())
            {
                // Convert RelatedItemInfo to IndexEventItemModel instances
                var relatedItemModels = new List<IIndexEventItemModel>();
                foreach (var relatedInfo in reusableItem.RelatedItems)
                {
                    // Create a minimal model for re-indexing - the actual data will be fetched during indexing.
                    // Only ItemID, ItemGuid, LanguageName, and ContentTypeName are needed to identify the item.
                    // Other fields use default values as they're not required for the re-indexing identification process.
                    relatedItemModels.Add(new IndexEventReusableItemModel(
                        relatedInfo.ItemID,
                        relatedInfo.ItemGuid,
                        relatedInfo.LanguageName,
                        relatedInfo.ContentTypeName,
                        string.Empty, // Name - not needed for identifying item to re-index
                        false, // IsSecured - will be determined when item is fetched during indexing
                        0, // ContentTypeID - will be determined when item is fetched during indexing
                        0 // ContentLanguageID - will be determined when item is fetched during indexing
                    ));
                }
                toReindex = relatedItemModels;
            }
            else
            {
                var strategy = serviceProvider.GetRequiredStrategy(luceneIndex);
                toReindex = await strategy.FindItemsToReindex(reusableItem);
            }

            if (toReindex is not null)
            {
                foreach (var item in toReindex)
                {
                    if (taskType == LuceneTaskType.DELETE)
                    {
                        LogIndexTaskInternal(item, LuceneTaskType.DELETE, luceneIndex.IndexName);
                    }
                    else
                    {
                        LogIndexTaskInternal(item, LuceneTaskType.UPDATE, luceneIndex.IndexName);
                    }
                }
            }

            if (taskType == LuceneTaskType.DELETE)
            {
                LogIndexTaskInternal(reusableItem, LuceneTaskType.DELETE, luceneIndex.IndexName);
            }
        }
    }

    /// <inheritdoc />
    public async Task HandleSecurityChangeEvent(IndexEventUpdateSecuritySettingsModel securityChangeItem, string eventName)
    {
        var webPageItemInfo = webPageItemInfoProvider
            .Get()
            .WhereEquals(nameof(WebPageItemInfo.WebPageItemID), securityChangeItem.ItemID)
            .SingleOrDefault();

        if (webPageItemInfo is null)
        {
            return;
        }

        var languageItemsWhereThisPageIsDefined = await contentLanguageInfoProvider
            .Get()
            .Source(x => x.InnerJoin<ContentItemLanguageMetadataInfo>(
                nameof(ContentLanguageInfo.ContentLanguageID),
                nameof(ContentItemLanguageMetadataInfo.ContentItemLanguageMetadataContentLanguageID)
            ))
            .WhereEquals(nameof(ContentItemLanguageMetadataInfo.ContentItemLanguageMetadataContentItemID), webPageItemInfo.WebPageItemContentItemID)
            .WhereEquals(nameof(ContentItemLanguageMetadataInfo.ContentItemLanguageMetadataLatestVersionStatus), VersionStatus.Published) // We only care about security changes for published items.
            .GetEnumerableTypedResultAsync();

        foreach (var item in languageItemsWhereThisPageIsDefined)
        {
            var webPageItem = new IndexEventWebPageItemModel(
                securityChangeItem.ItemID,
                securityChangeItem.ItemGuid,
                item.ContentLanguageName,
                securityChangeItem.ContentTypeName,
                securityChangeItem.Name,
                securityChangeItem.IsSecured,
                securityChangeItem.ContentTypeID,
                item.ContentLanguageID,
                securityChangeItem.WebsiteChannelName,
                securityChangeItem.WebPageItemTreePath,
                securityChangeItem.Order,
                securityChangeItem.ParentID
            );

            if (!securityChangeItem.IsSecured
                || (securityChangeItem.IsSecured && luceneSearchOptions.IncludeSecuredItems)
            )
            {
                await HandleEventInternal(webPageItem, eventName, LuceneTaskType.UPDATE);
            }
            else
            {
                await HandleEventInternal(webPageItem, eventName, LuceneTaskType.DELETE);
            }
        }
    }

    /// <inheritdoc />
    public void LogIndexTask(LuceneQueueItem task)
    {
        try
        {
            LuceneQueueWorker.EnqueueLuceneQueueItem(task);
        }
        catch (InvalidOperationException ex)
        {
            eventLogService.LogException(nameof(DefaultLuceneTaskLogger), nameof(LogIndexTask), ex);
        }
    }

    private void LogIndexTaskInternal(IIndexEventItemModel item, LuceneTaskType taskType, string indexName)
        => LogIndexTask(new LuceneQueueItem(item, taskType, indexName));

    private async Task HandleEventInternal(IndexEventWebPageItemModel webpageItem, string eventName, LuceneTaskType taskType)
    {
        foreach (var luceneIndex in indexManager.GetAllIndices())
        {
            if (!webpageItem.IsIndexedByIndex(eventLogService, indexManager, luceneIndex.IndexName, eventName))
            {
                continue;
            }

            IEnumerable<IIndexEventItemModel>? toReindex = null;

            // For delete events, use pre-captured RelatedItems if available to avoid querying deleted items
            if (taskType == LuceneTaskType.DELETE && webpageItem.RelatedItems.Any())
            {
                // Convert RelatedItemInfo to IndexEventItemModel instances
                var relatedItemModels = new List<IIndexEventItemModel>();
                foreach (var relatedInfo in webpageItem.RelatedItems)
                {
                    // Create a minimal model for re-indexing - the actual data will be fetched during indexing.
                    // Only ItemID, ItemGuid, LanguageName, and ContentTypeName are needed to identify the item.
                    // Other fields use default values as they're not required for the re-indexing identification process.
                    relatedItemModels.Add(new IndexEventWebPageItemModel(
                        relatedInfo.ItemID,
                        relatedInfo.ItemGuid,
                        relatedInfo.LanguageName,
                        relatedInfo.ContentTypeName,
                        string.Empty, // Name - not needed for identifying item to re-index
                        false, // IsSecured - will be determined when item is fetched during indexing
                        0, // ContentTypeID - will be determined when item is fetched during indexing
                        0, // ContentLanguageID - will be determined when item is fetched during indexing
                        string.Empty, // WebsiteChannelName - will be determined when item is fetched during indexing
                        string.Empty, // WebPageItemTreePath - will be determined when item is fetched during indexing
                        0, // Order - will be determined when item is fetched during indexing
                        null // ParentID - will be determined when item is fetched during indexing
                    ));
                }
                toReindex = relatedItemModels;
            }
            else
            {
                var strategy = serviceProvider.GetRequiredStrategy(luceneIndex);
                toReindex = await strategy.FindItemsToReindex(webpageItem);
            }

            if (toReindex is not null)
            {
                foreach (var item in toReindex)
                {
                    if (taskType == LuceneTaskType.DELETE)
                    {
                        LogIndexTaskInternal(item, LuceneTaskType.DELETE, luceneIndex.IndexName);
                    }
                    else
                    {
                        LogIndexTaskInternal(item, LuceneTaskType.UPDATE, luceneIndex.IndexName);
                    }
                }
            }

            if (taskType == LuceneTaskType.DELETE)
            {
                LogIndexTaskInternal(webpageItem, LuceneTaskType.DELETE, luceneIndex.IndexName);
            }
        }
    }

    private static LuceneTaskType GetTaskType(string eventName)
    {
        if (eventName.Equals(WebPageEvents.Publish.Name, StringComparison.OrdinalIgnoreCase)
            || eventName.Equals(ContentItemEvents.Publish.Name, StringComparison.OrdinalIgnoreCase))
        {
            return LuceneTaskType.UPDATE;
        }

        if (eventName.Equals(WebPageEvents.Delete.Name, StringComparison.OrdinalIgnoreCase) ||
            eventName.Equals(WebPageEvents.Unpublish.Name, StringComparison.OrdinalIgnoreCase) ||
            eventName.Equals(ContentItemEvents.Delete.Name, StringComparison.OrdinalIgnoreCase) ||
            eventName.Equals(ContentItemEvents.Unpublish.Name, StringComparison.OrdinalIgnoreCase))
        {
            return LuceneTaskType.DELETE;
        }

        return LuceneTaskType.UNKNOWN;
    }
}
