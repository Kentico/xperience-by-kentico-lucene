using System;
using System.Collections.Generic;
using System.Linq;

using CMS.DocumentEngine;

namespace Kentico.Xperience.Lucene.Models
{
    /// <summary>
    /// A queued item to be processed by <see cref="LuceneQueueWorker"/> which
    /// represents a recent change made to an indexed <see cref="TreeNode"/>.
    /// </summary>
    public sealed class LuceneQueueItem
    {
        /// <summary>
        /// The <see cref="TreeNode"/> that was changed.
        /// </summary>
        public TreeNode Node
        {
            get;
        }


        /// <summary>
        /// The type of the Lucene task.
        /// </summary>
        public LuceneTaskType TaskType
        {
            get;
        }


        /// <summary>
        /// The code name of the Lucene index to be updated.
        /// </summary>
        public string IndexName
        {
            get;
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="LuceneQueueItem"/> class.
        /// </summary>
        /// <param name="node">The <see cref="TreeNode"/> that was changed.</param>
        /// <param name="taskType">The type of the Lucene task.</param>
        /// <param name="indexName">The code name of the Lucene index to be updated.</param>
        /// <exception cref="ArgumentNullException" />
        public LuceneQueueItem(TreeNode node, LuceneTaskType taskType, string indexName)
        {
            if (String.IsNullOrEmpty(indexName))
            {
                throw new ArgumentNullException(nameof(indexName));
            }

            Node = node ?? throw new ArgumentNullException(nameof(node));
            TaskType = taskType;
            IndexName = indexName;
        }
    }
}