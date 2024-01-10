using CMS.Base;
using CMS.ContentEngine;
using CMS.Core;
using CMS.DataEngine;
using CMS.Websites;
using Kentico.Xperience.Lucene.Models;
using Kentico.Xperience.Lucene.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Kentico.Xperience.Lucene;

/// <summary>
/// Initializes page event handlers, and ensures the thread queue workers for processing Lucene tasks.
/// </summary>
internal class LuceneSearchModule : Module
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
    protected override void OnInit(ModuleInitParameters parameters)
    {
        base.OnInit();
        var services = parameters.Services;

        services.GetRequiredService<LuceneModuleInstaller>().Install();
        luceneTaskLogger = services.GetRequiredService<ILuceneTaskLogger>();
        appSettingsService = services.GetRequiredService<IAppSettingsService>();
        conversionService = services.GetRequiredService<IConversionService>();

        AddRegisteredIndices();
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
        var indexedItemModel = new IndexedItemModel(publishedEvent.ContentLanguageName,
            publishedEvent.ContentTypeName,
            publishedEvent.WebsiteChannelName,
            publishedEvent.Guid,
            publishedEvent.TreePath)
        {
            ID = publishedEvent.ID,
            ParentID = publishedEvent.ParentID,
            Name = publishedEvent.Name,
            Order = publishedEvent.Order,
            DisplayName = publishedEvent.DisplayName,
            IsSecured = publishedEvent.IsSecured,
            WebsiteChannelID = publishedEvent.WebsiteChannelID,
            ContentTypeID = publishedEvent.ContentTypeID,
            ContentLanguageID = publishedEvent.ContentLanguageID
        };

        luceneTaskLogger?.HandleEvent(indexedItemModel, e.CurrentHandler.Name).GetAwaiter().GetResult();
    }

    private void HandleContentItemEvent(object? sender, CMSEventArgs e)
    {
        if (IndexingDisabled)
        {
            return;
        }
        var publishedEvent = (ContentItemEventArgsBase)e;

        var indexedContentItemModel = new IndexedContentItemModel(publishedEvent.ContentLanguageName, publishedEvent.ContentTypeName, publishedEvent.ID, publishedEvent.Guid) 
        {
            Name = publishedEvent.Name,
            DisplayName = publishedEvent.DisplayName,
            IsSecured = publishedEvent.IsSecured,
            ContentTypeID = publishedEvent.ContentTypeID,
            ContentLanguageID = publishedEvent.ContentLanguageID,
        };

        luceneTaskLogger?.HandleContentItemEvent(indexedContentItemModel, e.CurrentHandler.Name).GetAwaiter().GetResult();
    }

    public static void AddRegisteredIndices()
    {
        var configurationStorageService = Service.Resolve<IConfigurationStorageService>();
        var indices = configurationStorageService.GetAllIndexData();

        IndexStore.Instance.AddIndices(indices);
    }
}
