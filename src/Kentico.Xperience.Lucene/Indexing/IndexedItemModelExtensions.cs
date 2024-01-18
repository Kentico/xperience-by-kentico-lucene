using CMS.Core;
using CMS.Websites.Internal;

namespace Kentico.Xperience.Lucene.Indexing;

/// <summary>
/// Lucene extension methods for the <see cref="IIndexEventItemModel"/> class.
/// </summary>
internal static class IndexedItemModelExtensions
{
    /// <summary>
    /// Returns true if the node is included in the Lucene index based on the index's defined paths
    /// </summary>
    /// <remarks>Logs an error if the search model cannot be found.</remarks>
    /// <param name="item">The node to check for indexing.</param>
    /// <param name="log"></param>
    /// <param name="indexName">The Lucene index code name.</param>
    /// <param name="eventName"></param>
    /// <exception cref="ArgumentNullException" />
    public static bool IsIndexedByIndex(this IndexEventWebPageItemModel item, IEventLogService log, string indexName, string eventName)
    {
        if (string.IsNullOrEmpty(indexName))
        {
            throw new ArgumentNullException(nameof(indexName));
        }
        if (item is null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        var luceneIndex = LuceneIndexStore.Instance.GetIndex(indexName);
        if (luceneIndex is null)
        {
            log.LogError(nameof(IndexedItemModelExtensions), nameof(IsIndexedByIndex), $"Error loading registered Lucene index '{indexName}' for event [{eventName}].");
            return false;
        }

        if (!luceneIndex.LanguageNames.Exists(x => x == item.LanguageName))
        {
            return false;
        }

        return luceneIndex.IncludedPaths.Any(includedPathAttribute =>
        {
            bool matchesContentType = includedPathAttribute.ContentTypes is not null
                && includedPathAttribute.ContentTypes.Contains(item.ContentTypeName, StringComparer.OrdinalIgnoreCase);

            if (!matchesContentType)
            {
                return false;
            }

            if (item.WebPageItemTreePath is null)
            {
                return false;
            }

            // Supports wildcard matching
            if (includedPathAttribute.AliasPath.EndsWith("/%", StringComparison.OrdinalIgnoreCase))
            {
                string pathToMatch = includedPathAttribute.AliasPath[..^2];
                var pathsOnPath = TreePathUtils.GetTreePathsOnPath(item.WebPageItemTreePath, true, false).ToHashSet();

                return pathsOnPath.Any(p => p.StartsWith(pathToMatch, StringComparison.OrdinalIgnoreCase));
            }

            return item.WebPageItemTreePath.Equals(includedPathAttribute.AliasPath, StringComparison.OrdinalIgnoreCase);
        });
    }

    /// <summary>
    /// Returns true if the node is included in the Lucene index's allowed
    /// </summary>
    /// <remarks>Logs an error if the search model cannot be found.</remarks>
    /// <param name="item">The node to check for indexing.</param>
    /// <param name="log"></param>
    /// <param name="indexName">The Lucene index code name.</param>
    /// <param name="eventName"></param>
    /// <exception cref="ArgumentNullException" />
    public static bool IsIndexedByIndex(this IndexEventReusableItemModel item, IEventLogService log, string indexName, string eventName)
    {
        if (string.IsNullOrEmpty(indexName))
        {
            throw new ArgumentNullException(nameof(indexName));
        }
        if (item is null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        var luceneIndex = LuceneIndexStore.Instance.GetIndex(indexName);
        if (luceneIndex is null)
        {
            log.LogError(nameof(IndexedItemModelExtensions), nameof(IsIndexedByIndex), $"Error loading registered Lucene index '{indexName}' for event [{eventName}].");
            return false;
        }

        if (luceneIndex.LanguageNames.Exists(x => x == item.LanguageName))
        {
            return true;
        }

        return false;
    }
}
