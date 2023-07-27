using CMS.Core;
using CMS.DocumentEngine;

using Kentico.Xperience.Lucene.Attributes;

namespace Kentico.Xperience.Lucene.Extensions
{
    /// <summary>
    /// Lucene extension methods for the <see cref="TreeNode"/> class.
    /// </summary>
    internal static class TreeNodeExtensions
    {
        /// <summary>
        /// Returns true if the node is included in any registered Lucene index.
        /// </summary>
        /// <param name="node">The <see cref="TreeNode"/> to check for indexing.</param>
        /// <exception cref="ArgumentNullException" />
        public static bool IsLuceneIndexed(this TreeNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            return IndexStore.Instance.GetAllIndexes().Any(index => node.IsIndexedByIndex(index.IndexName));
        }


        /// <summary>
        /// Returns true if the node is included in the Lucene index's allowed
        /// paths as set by the <see cref="IncludedPathAttribute"/>.
        /// </summary>
        /// <remarks>Logs an error if the search model cannot be found.</remarks>
        /// <param name="node">The node to check for indexing.</param>
        /// <param name="indexName">The Lucene index code name.</param>
        /// <exception cref="ArgumentNullException" />
        public static bool IsIndexedByIndex(this TreeNode node, string indexName)
        {
            if (string.IsNullOrEmpty(indexName))
            {
                throw new ArgumentNullException(nameof(indexName));
            }
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            var luceneIndex = IndexStore.Instance.GetIndex(indexName);
            if (luceneIndex == null)
            {
                Service.Resolve<IEventLogService>().LogError(nameof(TreeNodeExtensions), nameof(IsIndexedByIndex), $"Error loading registered Lucene index '{indexName}.'");
                return false;
            }

            if (!luceneIndex.LuceneIndexingStrategy.ShouldIndexNode(node))
            {
                return false;
            }

            return luceneIndex.IncludedPaths.Any(includedPathAttribute =>
            {
                bool matchesContentType = includedPathAttribute.ContentTypes == null || includedPathAttribute.ContentTypes.Length == 0 || includedPathAttribute.ContentTypes.Contains(node.ClassName);
                if (includedPathAttribute.AliasPath.EndsWith("/%"))
                {
                    string? pathToMatch = TreePathUtils.EnsureSingleNodePath(includedPathAttribute.AliasPath);
                    var pathsOnPath = TreePathUtils.GetNodeAliasPathsOnPath(node.NodeAliasPath, true, false).ToHashSet();

                    return pathsOnPath.Contains(pathToMatch) && matchesContentType;
                }
                else
                {
                    return node.NodeAliasPath.Equals(includedPathAttribute.AliasPath, StringComparison.OrdinalIgnoreCase) && matchesContentType;
                }
            });
        }
    }
}
