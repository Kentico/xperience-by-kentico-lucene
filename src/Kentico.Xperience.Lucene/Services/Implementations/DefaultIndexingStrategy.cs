using CMS.DocumentEngine;
using Kentico.Xperience.Lucene.Models;

namespace Kentico.Xperience.Lucene.Services.Implementations
{
    /// <summary>
    /// Default indexing startegy just implements the methods but does not change the data.
    /// </summary>
    public class DefaultLuceneIndexingStrategy : ILuceneIndexingStrategy
    {
        /// <inheritdoc />
        public virtual Task<object?> OnIndexingProperty(TreeNode node, string propertyName, string usedColumn, object? foundValue) => Task.FromResult(foundValue);

        /// <inheritdoc />
        public virtual Task<LuceneSearchModel> OnIndexingNode(TreeNode node, LuceneSearchModel model) => Task.FromResult(model);

        /// <inheritdoc />
        public virtual bool ShouldIndexNode(TreeNode node) => true;
    }
}
