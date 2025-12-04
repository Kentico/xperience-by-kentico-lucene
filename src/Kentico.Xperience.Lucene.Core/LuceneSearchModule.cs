using CMS;
using CMS.Base;
using CMS.ContentEngine;
using CMS.Core;
using CMS.DataEngine;
using CMS.Websites;

using Kentico.Xperience.Lucene.Core;
using Kentico.Xperience.Lucene.Core.Indexing;
using Kentico.Xperience.Lucene.Core.Scaling;

using Microsoft.Extensions.DependencyInjection;

[assembly: RegisterModule(typeof(LuceneSearchModule))]

namespace Kentico.Xperience.Lucene.Core;

/// <summary>
/// Initializes page event handlers, and ensures the thread queue workers for processing Lucene tasks.
/// </summary>
internal class LuceneSearchModule : Module
{
    private ILuceneTaskLogger luceneTaskLogger = null!;
    private IAppSettingsService appSettingsService = null!;
    private IConversionService conversionService = null!;
    private IWebFarmService webFarmService = null!;
    private LuceneModuleInstaller installer = null!;
    private LuceneSearchOptions luceneSearchOptions = null!;
    private IServiceProvider serviceProvider = null!;
    private ILuceneIndexManager indexManager = null!;

    private const string APP_SETTINGS_KEY_INDEXING_DISABLED = "CMSLuceneSearchDisableIndexing";

    private bool IndexingDisabled
    {
        get
        {
            if (appSettingsService[APP_SETTINGS_KEY_INDEXING_DISABLED] is string value1)
            {
                return conversionService.GetBoolean(value1, false);
            }

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

        installer = services.GetRequiredService<LuceneModuleInstaller>();

        ApplicationEvents.Initialized.Execute += InitializeModule;
        ApplicationEvents.Initialized.Execute += RegisterLuceneWebFarmTasks;

        luceneTaskLogger = services.GetRequiredService<ILuceneTaskLogger>();
        appSettingsService = services.GetRequiredService<IAppSettingsService>();
        conversionService = services.GetRequiredService<IConversionService>();
        webFarmService = services.GetRequiredService<IWebFarmService>();
        serviceProvider = services;
        indexManager = services.GetRequiredService<ILuceneIndexManager>();

        luceneSearchOptions = new LuceneSearchOptions
        {
            IncludeSecuredItems = conversionService.GetBoolean(
                appSettingsService[$"{LuceneSearchOptions.CMS_LUCENE_SEARCH_SECTION_NAME}:{nameof(LuceneSearchOptions.IncludeSecuredItems)}"],
                false
            )
        };

        WebPageEvents.UpdateSecuritySettings.After += HandleEvent;
        WebPageEvents.Publish.Execute += HandleEvent;
        WebPageEvents.Delete.Execute += HandleEvent;
        WebPageEvents.Unpublish.Execute += HandleEvent;
        ContentItemEvents.Publish.Execute += HandleContentItemEvent;
        ContentItemEvents.Delete.Execute += HandleContentItemEvent;
        ContentItemEvents.Unpublish.Execute += HandleContentItemEvent;

        RequestEvents.RunEndRequestTasks.Execute += (sender, eventArgs) => LuceneQueueWorker.Current.EnsureRunningThread();
    }

    private void HandleEvent(object? sender, UpdateWebPageSecuritySettingsEventsArgs e)
    {
        if (IndexingDisabled)
        {
            return;
        }

        var indexedItemModel = new IndexEventUpdateSecuritySettingsModel(
            e.ID,
            e.Guid,
            e.ContentTypeName,
            e.Name,
            e.IsSecured,
            e.ContentTypeID,
            e.WebsiteChannelName,
            e.TreePath,
            e.ParentID,
            e.Order
        );

        luceneTaskLogger?.HandleSecurityChangeEvent(indexedItemModel, e.CurrentHandler.Name).GetAwaiter().GetResult();
    }


    /// <summary>
    /// Called when a page is published. Logs an Lucene task to be processed later.
    /// </summary>
    private void HandleEvent(object? sender, CMSEventArgs e)
    {
        if (IndexingDisabled
            || e is not WebPageEventArgsBase publishedEvent
            || (publishedEvent.IsSecured && !luceneSearchOptions.IncludeSecuredItems)
        )
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

        // For delete events, capture related items while the item still exists in the database
        if (e.CurrentHandler.Name.Equals(WebPageEvents.Delete.Name, StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var relatedItems = new List<RelatedItemInfo>();
                foreach (var luceneIndex in indexManager.GetAllIndices())
                {
                    var strategy = serviceProvider.GetRequiredStrategy(luceneIndex);
                    var toReindex = strategy.FindItemsToReindex(indexedItemModel).GetAwaiter().GetResult();
                    if (toReindex != null)
                    {
                        foreach (var item in toReindex)
                        {
                            relatedItems.Add(new RelatedItemInfo(
                                item.ItemID,
                                item.ItemGuid,
                                item.ContentTypeName,
                                item.LanguageName
                            ));
                        }
                    }
                }
                indexedItemModel.RelatedItems = relatedItems;
            }
            catch
            {
                // Silently fail to avoid disrupting the deletion process
            }
        }

        luceneTaskLogger?.HandleEvent(indexedItemModel, e.CurrentHandler.Name).GetAwaiter().GetResult();
    }


    private void HandleContentItemEvent(object? sender, CMSEventArgs e)
    {
        if (IndexingDisabled
            || e is not ContentItemEventArgsBase publishedEvent
            || (publishedEvent.IsSecured && !luceneSearchOptions.IncludeSecuredItems)
        )
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

        // For delete events, capture related items while the item still exists in the database
        if (e.CurrentHandler.Name.Equals(ContentItemEvents.Delete.Name, StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var relatedItems = new List<RelatedItemInfo>();
                foreach (var luceneIndex in indexManager.GetAllIndices())
                {
                    var strategy = serviceProvider.GetRequiredStrategy(luceneIndex);
                    var toReindex = strategy.FindItemsToReindex(indexedContentItemModel).GetAwaiter().GetResult();
                    if (toReindex != null)
                    {
                        foreach (var item in toReindex)
                        {
                            relatedItems.Add(new RelatedItemInfo(
                                item.ItemID,
                                item.ItemGuid,
                                item.ContentTypeName,
                                item.LanguageName
                            ));
                        }
                    }
                }
                indexedContentItemModel.RelatedItems = relatedItems;
            }
            catch
            {
                // Silently fail to avoid disrupting the deletion process
            }
        }

        luceneTaskLogger?.HandleReusableItemEvent(indexedContentItemModel, e.CurrentHandler.Name).GetAwaiter().GetResult();
    }

    private void InitializeModule(object? sender, EventArgs e) =>
       installer.Install();

    private void RegisterLuceneWebFarmTasks(object? sender, EventArgs e)
    {
        webFarmService.RegisterTask<DeleteIndexWebFarmTask>();
        webFarmService.RegisterTask<ProcessLuceneTasksWebFarmTask>();
        webFarmService.RegisterTask<ResetIndexWebFarmTask>();
    }
}
