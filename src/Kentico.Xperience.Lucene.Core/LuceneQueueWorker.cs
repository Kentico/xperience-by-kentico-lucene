using CMS.Base;
using CMS.Core;

using Kentico.Xperience.Lucene.Core.Indexing;
using Kentico.Xperience.Lucene.Core.Scaling;

namespace Kentico.Xperience.Lucene.Core;

/// <summary>
/// Thread worker which enqueues recently updated or deleted nodes indexed
/// by Lucene and processes the tasks in the background thread.
/// </summary>
internal class LuceneQueueWorker : ThreadQueueWorker<LuceneQueueItem, LuceneQueueWorker>
{
    private readonly ILuceneTaskProcessor luceneTaskProcessor;
    private readonly IWebFarmService webFarmService;

    /// <inheritdoc />
    protected override int DefaultInterval => 10000;


    /// <summary>
    /// Initializes a new instance of the <see cref="LuceneQueueWorker"/> class.
    /// Should not be called directly- the worker should be initialized during startup using
    /// <see cref="ThreadWorker{T}.EnsureRunningThread"/>.
    /// </summary>
    public LuceneQueueWorker()
    {
        luceneTaskProcessor = Service.Resolve<ILuceneTaskProcessor>() ?? throw new InvalidOperationException($"{nameof(ILuceneTaskProcessor)} is not registered.");
        webFarmService = Service.Resolve<IWebFarmService>();
    }


    /// <summary>
    /// Adds an <see cref="LuceneQueueItem"/> to the worker queue to be processed.
    /// </summary>
    /// <param name="queueItem">The item to be added to the queue.</param>
    /// <exception cref="InvalidOperationException" />
    public static void EnqueueLuceneQueueItem(LuceneQueueItem queueItem)
    {
        if (queueItem == null || (queueItem.ItemToIndex == null && queueItem.TaskType != LuceneTaskType.PUBLISH_INDEX) || string.IsNullOrEmpty(queueItem.IndexName))
        {
            return;
        }

        if (queueItem.TaskType == LuceneTaskType.UNKNOWN)
        {
            return;
        }

        var indexManager = Service.Resolve<ILuceneIndexManager>() ?? throw new InvalidOperationException($"{nameof(ILuceneIndexManager)} is not registered.");

        if (indexManager.GetIndex(queueItem.IndexName) == null)
        {
            throw new InvalidOperationException($"Attempted to log task for Lucene index '{queueItem.IndexName},' but it is not registered.");
        }

        Current.Enqueue(queueItem, false);
    }


    /// <inheritdoc />
    protected override void Finish() => RunProcess();


    /// <inheritdoc/>
    protected override void ProcessItem(LuceneQueueItem item)
    {
    }


    /// <inheritdoc />
    protected override int ProcessItems(IEnumerable<LuceneQueueItem> items)
    {
        var itemList = items.ToList();

        if (itemList.Count == 0)
        {
            return 0;
        }
        webFarmService.CreateTask(new ProcessLuceneTasksWebFarmTask
        {
            LuceneQueueItems = itemList,
            CreatorName = webFarmService.ServerName
        });

        return luceneTaskProcessor.ProcessLuceneTasks(itemList, CancellationToken.None).GetAwaiter().GetResult();
    }
}
