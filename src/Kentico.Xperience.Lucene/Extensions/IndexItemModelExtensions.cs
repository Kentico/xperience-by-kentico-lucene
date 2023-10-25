using CMS.Core;
using CMS.Websites;
using CMS.Websites.Internal;
using Kentico.Xperience.Lucene.Attributes;
using Kentico.Xperience.Lucene.Models;

namespace Kentico.Xperience.Lucene.Extensions;

/// <summary>
/// Lucene extension methods for the <see cref="IndexedItemModel"/> class.
/// </summary>
internal static class IndexItemModelExtensions
{
    /// <summary>
    /// Returns true if the indexedItem is included in any registered Lucene index.
    /// </summary>
    /// <param name="indexedItem">The <see cref="IndexedItemModel"/> to check for indexing.</param>
    /// <exception cref="ArgumentNullException" />
    public static async Task<bool> IsLuceneIndexed(this IndexedItemModel indexedItem, string eventName)
    {
        if (indexedItem == null)
        {
            throw new ArgumentNullException(nameof(indexedItem));
        }

        foreach (var index in IndexStore.Instance.GetAllIndexes())
        {
            if (await indexedItem.IsIndexedByIndex(index.IndexName, eventName))
            {
                return true;
            }
        }

        return false;
    }


    /// <summary>
    /// Returns true if the indexedItem is included in the Lucene index's allowed
    /// paths as set by the <see cref="IncludedPathAttribute"/>.
    /// </summary>
    /// <remarks>Logs an error if the search model cannot be found.</remarks>
    /// <param name="indexedItem">The indexedItem to check for indexing.</param>
    /// <param name="indexName">The Lucene index code name.</param>
    /// <exception cref="ArgumentNullException" />
    public static async Task<bool> IsIndexedByIndex(this IndexedItemModel indexedItem, string indexName, string eventName)
    {
        if (string.IsNullOrEmpty(indexName))
        {
            throw new ArgumentNullException(nameof(indexName));
        }
        if (indexedItem == null)
        {
            throw new ArgumentNullException(nameof(indexedItem));
        }

        var luceneIndex = IndexStore.Instance.GetIndex(indexName);
        if (luceneIndex == null)
        {
            Service.Resolve<IEventLogService>().LogError(nameof(IndexItemModelExtensions), nameof(IsIndexedByIndex), $"Error loading registered Lucene index '{indexName}.'");
            return false;
        }

        if (!await luceneIndex.LuceneIndexingStrategy.ShouldIndexNode(indexedItem) && eventName != WebPageEvents.Delete.Name)
        {
            return false;
        }

        return luceneIndex.IncludedPaths.Any(includedPathAttribute =>
        {
            bool matchesContentType = includedPathAttribute.ContentTypes == null || includedPathAttribute.ContentTypes.Length == 0 || includedPathAttribute.ContentTypes.Contains(indexedItem.TypeName);
            if (includedPathAttribute.AliasPath.EndsWith("/"))
            {
                string? pathToMatch = includedPathAttribute.AliasPath;
                var pathsOnPath = TreePathUtils.GetTreePathsOnPath(indexedItem.WebPageItemTreePath, true, false).ToHashSet();

                return pathsOnPath.Contains(pathToMatch) && matchesContentType;
            }
            else
            {
                return indexedItem.WebPageItemTreePath.Equals(includedPathAttribute.AliasPath, StringComparison.OrdinalIgnoreCase) && matchesContentType;
            }
        });
    }
}
