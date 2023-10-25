using CMS.Base;
using CMS.Core;
using CMS.DataEngine;
using CMS.Websites;
using Kentico.Xperience.Lucene.Models;
using Kentico.Xperience.Lucene.Services;

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
    protected override void OnInit()
    {
        base.OnInit();

        luceneTaskLogger = Service.Resolve<ILuceneTaskLogger>();
        appSettingsService = Service.Resolve<IAppSettingsService>();
        conversionService = Service.Resolve<IConversionService>();

        WebPageEvents.Publish.Execute += HandleEvent;
        WebPageEvents.Delete.Execute += HandleEvent;
        RequestEvents.RunEndRequestTasks.Execute += (sender, eventArgs) => LuceneQueueWorker.Current.EnsureRunningThread();
    }

    private void Delete_Execute(object sender, DeleteWebPageEventArgs e)
    {
        throw new System.NotImplementedException();
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
        WebPageEventArgsBase publishedEvent = (WebPageEventArgsBase) e;
        var indexedItemModel = new IndexedItemModel
        {
            LanguageName = publishedEvent.ContentLanguageName,
            TypeName = publishedEvent.ContentTypeName,
            ChannelName = publishedEvent.WebsiteChannelName,
            WebPageItemGuid = publishedEvent.Guid,
            WebPageItemTreePath = publishedEvent.TreePath,
        };

        var task = luceneTaskLogger?.HandleEvent(indexedItemModel, e.CurrentHandler.Name);
        task?.Wait();
    }
}
