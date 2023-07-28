using CMS.Base;
using CMS.Core;
using CMS.DataEngine;
using CMS.DocumentEngine;

using Kentico.Xperience.Lucene.Services;

namespace Kentico.Xperience.Lucene
{
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

            DocumentEvents.Delete.Before += HandleDocumentEvent;
            WorkflowEvents.Publish.After += HandleWorkflowEvent;
            WorkflowEvents.Archive.Before += HandleWorkflowEvent;
            RequestEvents.RunEndRequestTasks.Execute += (sender, eventArgs) => LuceneQueueWorker.Current.EnsureRunningThread();
            //RequestEvents.RunEndRequestTasks.Execute += (sender, eventArgs) => LuceneCrawlerQueueWorker.Current.EnsureRunningThread();
        }


        /// <summary>
        /// Called when a page is published or archived. Logs an Lucene task to be processed later.
        /// </summary>
        private void HandleWorkflowEvent(object? sender, WorkflowEventArgs e)
        {
            if (IndexingDisabled)
            {
                return;
            }

            luceneTaskLogger?.HandleEvent(e.Document, e.CurrentHandler.Name);
        }


        /// <summary>
        /// Called when a page is deleted. Logs an Lucene task to be processed later.
        /// </summary>
        private void HandleDocumentEvent(object? sender, DocumentEventArgs e)
        {
            if (IndexingDisabled)
            {
                return;
            }

            luceneTaskLogger?.HandleEvent(e.Node, e.CurrentHandler.Name);
        }
    }
}
