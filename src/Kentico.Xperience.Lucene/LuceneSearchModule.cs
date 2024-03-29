using CMS;
using CMS.Base;
using CMS.ContentEngine;
using CMS.Core;
using CMS.DataEngine;
using CMS.Websites;
using Kentico.Xperience.Lucene;
using Kentico.Xperience.Lucene.Indexing;
using Microsoft.Extensions.DependencyInjection;

[assembly: RegisterModule(typeof(LuceneSearchModule))]

namespace Kentico.Xperience.Lucene;

/// <summary>
/// Initializes page event handlers, and ensures the thread queue workers for processing Lucene tasks.
/// </summary>
internal class LuceneSearchModule : Module
{
    private ILuceneTaskLogger luceneTaskLogger = null!;
    private IAppSettingsService appSettingsService = null!;
    private IConversionService conversionService = null!;

    [Obsolete("This setting will be replaced in the next major version. Use CMSLuceneSearchDisableIndexing instead.")]
    private const string APP_SETTINGS_KEY_INDEXING_DISABLED_OLD = "LuceneSearchDisableIndexing";
    private const string APP_SETTINGS_KEY_INDEXING_DISABLED = "CMSLuceneSearchDisableIndexing";

    private bool IndexingDisabled
    {
        get
        {
            if (appSettingsService[APP_SETTINGS_KEY_INDEXING_DISABLED] is string value1)
            {
                return conversionService.GetBoolean(value1, false);
            }

#pragma warning disable CS0618 // Type or member is obsolete
            if (appSettingsService[APP_SETTINGS_KEY_INDEXING_DISABLED_OLD] is string value2)
            {
                return conversionService.GetBoolean(value2, false);
            }
#pragma warning restore CS0618 // Type or member is obsolete

            return false;
        }
    }

    /// <inheritdoc/>
    public LuceneSearchModule() : base(nameof(LuceneSearchModule))
    {
    }

    /// <inheritdoc/>
    protected override void OnInit(ModuleInitParameters parameters)
    {
        base.OnInit(parameters);

        var services = parameters.Services;

        luceneTaskLogger = services.GetRequiredService<ILuceneTaskLogger>();
        appSettingsService = services.GetRequiredService<IAppSettingsService>();
        conversionService = services.GetRequiredService<IConversionService>();

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
        if (IndexingDisabled || e is not WebPageEventArgsBase publishedEvent)
        {
            return;
        }

        var indexedItemModel = new IndexEventWebPageItemModel(
            publishedEvent.ID,
            publishedEvent.Guid,
            publishedEvent.ContentLanguageName,
            publishedEvent.ContentTypeName,
            publishedEvent.Name,
            publishedEvent.IsSecured,
            publishedEvent.ContentTypeID,
            publishedEvent.ContentLanguageID,
            publishedEvent.WebsiteChannelName,
            publishedEvent.TreePath,
            publishedEvent.ParentID,
            publishedEvent.Order)
        { };

        luceneTaskLogger?.HandleEvent(indexedItemModel, e.CurrentHandler.Name).GetAwaiter().GetResult();
    }

    private void HandleContentItemEvent(object? sender, CMSEventArgs e)
    {
        if (IndexingDisabled || e is not ContentItemEventArgsBase publishedEvent)
        {
            return;
        }

        var indexedContentItemModel = new IndexEventReusableItemModel(
            publishedEvent.ID,
            publishedEvent.Guid,
            publishedEvent.ContentLanguageName,
            publishedEvent.ContentTypeName,
            publishedEvent.Name,
            publishedEvent.IsSecured,
            publishedEvent.ContentTypeID,
            publishedEvent.ContentLanguageID
        );

        luceneTaskLogger?.HandleReusableItemEvent(indexedContentItemModel, e.CurrentHandler.Name).GetAwaiter().GetResult();
    }
}
