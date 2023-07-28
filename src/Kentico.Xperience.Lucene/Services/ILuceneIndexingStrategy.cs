using CMS.DocumentEngine;
using Kentico.Xperience.Lucene.Attributes;
using Kentico.Xperience.Lucene.Models;

namespace Kentico.Xperience.Lucene.Services
{
    public interface ILuceneIndexingStrategy
    {
        /// <summary>
        /// Called when indexing a search model property. Does not trigger when indexing the
        /// properties specified by <see cref="LuceneSearchModel"/> base class.
        /// </summary>
        /// <param name="node">The <see cref="TreeNode"/> currently being indexed.</param>
        /// <param name="propertyName">The search model property that is being indexed.</param>
        /// <param name="usedColumn">The column that the value was retrieved from when the
        /// property uses the <see cref="SourceAttribute"/>. If not used, the parameter will
        /// be null.</param>
        /// <param name="foundValue">The value of the property that was found in the <paramref name="node"/>,
        /// or null if no value was found.</param>
        /// <returns>The value that will be indexed in Lucene.</returns>
        Task<object?> OnIndexingProperty(TreeNode node, string propertyName, string usedColumn, object? foundValue);

        /// <summary>
        /// Called when indexing a search model. Enables overriding of multiple fields with custom data.
        /// </summary>
        /// <param name="node">The <see cref="TreeNode"/> currently being indexed.</param>
        /// <param name="model">The resulting search data <see cref="LuceneSearchModel"/> to be modified. The model could be changed during the process.</param>
        /// <returns>Modified Lucene document.</returns>
        Task<LuceneSearchModel> OnIndexingNode(TreeNode node, LuceneSearchModel model);

        /// <summary>
        /// Called when indexing a search model. Could be used to disable indexing of documents that match the scope, but should not be indexed e.g. error pages.
        /// </summary>
        /// <param name="node">The <see cref="TreeNode"/> currently being indexed.</param>
        /// <returns>bool</returns>
        bool ShouldIndexNode(TreeNode node);
    }
}
