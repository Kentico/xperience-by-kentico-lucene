using CMS.Core;
using CMS.DataEngine;
using CMS.FormEngine;
using CMS.MediaLibrary;

using Kentico.Content.Web.Mvc;
using Kentico.Xperience.Lucene.Models;
using CMS.Websites;

namespace Kentico.Xperience.Lucene.Services;

/// <summary>
/// Default implementation of <see cref="ILuceneModelGenerator"/>.
/// </summary>
internal class DefaultLuceneModelGenerator : ILuceneModelGenerator
{
    private readonly IConversionService conversionService;
    private readonly IEventLogService eventLogService;
    private readonly IWebPageUrlRetriever urlRetriever;
    private readonly IMediaFileInfoProvider mediaFileInfoProvider;
    private readonly IMediaFileUrlRetriever mediaFileUrlRetriever;
    private readonly Dictionary<string, string[]> cachedIndexedColumns = new();
    private readonly string[] ignoredPropertiesForTrackingChanges = new string[] {
        nameof(LuceneSearchModel.ObjectID),
        nameof(LuceneSearchModel.Url),
        nameof(LuceneSearchModel.ClassName)
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultLuceneModelGenerator"/> class.
    /// </summary>
    public DefaultLuceneModelGenerator(IConversionService conversionService,
        IEventLogService eventLogService,
        IWebPageUrlRetriever urlRetriever,
        IMediaFileInfoProvider mediaFileInfoProvider,
        IMediaFileUrlRetriever mediaFileUrlRetriever)
    {
        this.conversionService = conversionService;
        this.eventLogService = eventLogService;
        this.urlRetriever = urlRetriever;
        this.mediaFileInfoProvider = mediaFileInfoProvider;
        this.mediaFileUrlRetriever = mediaFileUrlRetriever;
    }


    /// <inheritdoc/>
    public async Task<LuceneSearchModel> GetWebPageItemData(LuceneQueueItem queueItem)
    {
        var luceneIndex = IndexStore.Instance.GetIndex(queueItem.IndexName) ?? throw new Exception($"LuceneIndex {queueItem.IndexName} not found!");

        var data = Activator.CreateInstance(luceneIndex.LuceneSearchModelType) as LuceneSearchModel ?? throw new Exception($"Faild to create instance of {luceneIndex.LuceneSearchModelType}");

        //await MapChangedProperties(luceneIndex, queueItem, data!, queueItem.Language);
        await MapCommonProperties(queueItem.IndexedItemModel, data!, queueItem.Language);
        data = await luceneIndex.LuceneIndexingStrategy.OnIndexingNode(queueItem.IndexedItemModel, data);
        return data;
    }

    /// <summary>
    /// Converts the assets from the <paramref name="pageContentContainer"/>'s value into absolute URLs.
    /// </summary>
    /// <remarks>Logs an error if the definition of the <paramref name="columnName"/> can't
    /// be found.</remarks>
    /// <param name="pageContentContainer">The <see cref="IWebPageContentQueryDataContainer"/> the value was loaded from.</param>
    /// <param name="nodeValue">The original value of the column.</param>
    /// <param name="columnName">The name of the column the value was loaded from.</param>
    /// <returns>An list of absolute URLs, or an empty list.</returns>
    private IEnumerable<string> GetAssetUrlsForColumn(IWebPageContentQueryDataContainer pageContentContainer, object nodeValue, string columnName)
    {
        string strValue = conversionService.GetString(nodeValue, string.Empty);
        if (string.IsNullOrEmpty(strValue))
        {
            return Enumerable.Empty<string>();
        }

        // Ensure field is Asset type
        var dataClassInfo = DataClassInfoProvider.GetDataClassInfo(pageContentContainer.GetType().Name, false);
        var formInfo = new FormInfo(dataClassInfo.ClassFormDefinition);
        var field = formInfo.GetFormField(columnName);
        if (field == null)
        {
            eventLogService.LogError(nameof(DefaultLuceneModelGenerator), nameof(GetAssetUrlsForColumn), $"Unable to load field definition for content type '{pageContentContainer.GetType().Name}' column name '{columnName}.'");
            return Enumerable.Empty<string>();
        }

        if (!field.DataType.Equals(FieldDataType.Assets, StringComparison.OrdinalIgnoreCase))
        {
            return Enumerable.Empty<string>();
        }

        var dataType = DataTypeManager.GetDataType(typeof(IEnumerable<AssetRelatedItem>));
        if (dataType.Convert(strValue, null, null) is not IEnumerable<AssetRelatedItem> assets)
        {
            return Enumerable.Empty<string>();
        }

        var mediaFiles = mediaFileInfoProvider.Get().ForAssets(assets);

        return mediaFiles.Select(file => mediaFileUrlRetriever.Retrieve(file).RelativePath);
    }

    /// <summary>
    /// Sets values in the <paramref name="data"/> object using the common search model properties
    /// located within the <see cref="LuceneSearchModel"/> class.
    /// </summary>
    /// <param name="pageContentContainer">The <see cref="IWebPageContentQueryDataContainer"/> to load values from.</param>
    /// <param name="data">The data object based on <see cref="LuceneSearchModel"/>.</param>
    /// <param name="languageName">The language on the WebSite which is indexed.</param>
    private async Task MapCommonProperties(IndexedItemModel lucenePageItem, LuceneSearchModel data, string languageName)
    {
        data.ClassName = lucenePageItem.TypeName;
        data.ObjectID = lucenePageItem.WebPageItemGuid.ToString();

        string url;
        try
        {
            url = (await urlRetriever.Retrieve(lucenePageItem.WebPageItemGuid, languageName)).RelativePath;
        }
        catch (Exception)
        {
            // Retrieve can throw an exception when processing a page update LuceneQueueItem
            // and the page was deleted before the update task has processed. In this case, upsert an
            // empty URL
            url = string.Empty;
        }

        data.Url = url;
    }
}
