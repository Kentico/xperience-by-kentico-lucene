using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CMS.DocumentEngine;

using Kentico.Xperience.Lucene.Models;

using Newtonsoft.Json.Linq;

namespace Kentico.Xperience.Lucene.Services
{
    /// <summary>
    /// Generates <see cref="LuceneSearchModel"/>s from Xperience <see cref="TreeNode"/>s
    /// for upserting into Lucene.
    /// </summary>
    public interface ILuceneModelGenerator
    {
        /// <summary>
        /// Creates an anonymous object with the indexed column names of the and their values loaded from the passed
        /// <see cref="LuceneQueueItem.Node"/>.
        /// </summary>
        /// <param name="queueItem">The queue item to process.</param>
        /// <returns>The anonymous data that will be passed to Lucene.</returns>
        /// <exception cref="ArgumentNullException" />
        Task<LuceneSearchModel> GetTreeNodeData(LuceneQueueItem queueItem);
    }
}
