using CMS.Base;
using CMS.ContentEngine;
using CMS.Core;
using CMS.Websites;
using Kentico.Xperience.Lucene.Models;
using Kentico.Xperience.Lucene.Services;
using System.Threading.Tasks;

namespace Kentico.Xperience.Lucene;

/// <summary>
/// Initializes page event handlers, and ensures the thread queue workers for processing Lucene tasks.
/// </summary>
internal class LuceneSearchModule : CMS.DataEngine.Module
{
    private ILuceneTaskLogger? luceneTaskLogger;
    private IAppSettingsService? appSettingsService;
    private IConversionService? conversionService;
    private const string APP_SETTINGS_KEY_INDEXING_DISABLED = "LuceneSearchDisableIndexing";

    private bool IndexingDisabled => conversionService?.GetBoolean(appSettingsService?[APP_SETTINGS_KEY_INDEXING_DISABLED], false) ?? false;

    /// <inheritdoc/>
    public LuceneSearchModule() : base(nameof(LuceneSearchModule))
    {
    }

    /// <inheritdoc/>
    protected override void OnInit()
    {
        base.OnInit();
        Service.Resolve<LuceneModuleInstaller>().Install();
        luceneTaskLogger = Service.Resolve<ILuceneTaskLogger>();
        appSettingsService = Service.Resolve<IAppSettingsService>();
        conversionService = Service.Resolve<IConversionService>();

        AddRegisteredIndices().Wait();
        WebPageEvents.Publish.Execute += HandleEvent;
        WebPageEvents.Delete.Execute += HandleEvent;
        ContentItemEvents.Publish.Execute += HandleContentItemEvent;
        ContentItemEvents.Delete.Execute += HandleContentItemEvent;

        RequestEvents.RunEndRequestTasks.Execute += (sender, eventArgs) => LuceneQueueWorker.Current.EnsureRunningThread();
    }

    /// <summary>
    /// Called when a page is published. Logs an Lucene task to be processed later.
    /// </summary>
    private void HandleEvent(object? sender, CMSEventArgs e)
    {
        if (IndexingDisabled)
        {
            return;
        }
        var publishedEvent = (WebPageEventArgsBase)e;
        var indexedItemModel = new IndexedItemModel
        {
            LanguageCode = publishedEvent.ContentLanguageName,
            ClassName = publishedEvent.ContentTypeName,
            ChannelName = publishedEvent.WebsiteChannelName,
            WebPageItemGuid = publishedEvent.Guid,
            WebPageItemTreePath = publishedEvent.TreePath,
        };

        var task = luceneTaskLogger?.HandleEvent(indexedItemModel, e.CurrentHandler.Name);
        task.Wait();
    }

    private void HandleContentItemEvent(object? sender, CMSEventArgs e)
    {
        if (IndexingDisabled)
        {
            return;
        }
        var publishedEvent = (ContentItemEventArgsBase)e;

        var indexedContentItemModel = new IndexedContentItemModel
        {
            LanguageCode = publishedEvent.ContentLanguageName,
            ClassName = publishedEvent.ContentTypeName,
            ContentItemGuid = publishedEvent.Guid
        };

        var task = luceneTaskLogger?.HandleContentItemEvent(indexedContentItemModel, e.CurrentHandler.Name);

        task.Wait();
    }

    public static async Task AddRegisteredIndices()
    {
        var configurationStorageService = Service.Resolve<IConfigurationStorageService>();
        var indices = await configurationStorageService.GetAllIndexData();

        IndexStore.Instance.AddIndices(indices);
    }
}
