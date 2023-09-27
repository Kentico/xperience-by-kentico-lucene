using CMS.Core;
using CMS.DocumentEngine;
using CMS.Websites;
using Kentico.Xperience.Lucene.Attributes;

namespace Kentico.Xperience.Lucene.Extensions;

/// <summary>
/// Lucene extension methods for the <see cref="IWebPageFieldsSource"/> class.
/// </summary>
internal static class IWebPageContentQueryDataContainerExtensions
{
    /// <summary>
    /// Returns true if the node is included in any registered Lucene index.
    /// </summary>
    /// <param name="webPageItem">The <see cref="IWebPageFieldsSource"/> to check for indexing.</param>
    /// <exception cref="ArgumentNullException" />
    public static bool IsLuceneIndexed(this IWebPageContentQueryDataContainer webPageItem)
    {
        if (webPageItem == null)
        {
            throw new ArgumentNullException(nameof(webPageItem));
        }

        return IndexStore.Instance.GetAllIndexes().Any(index => webPageItem.IsIndexedByIndex(index.IndexName));
    }


    /// <summary>
    /// Returns true if the node is included in the Lucene index's allowed
    /// paths as set by the <see cref="IncludedPathAttribute"/>.
    /// </summary>
    /// <remarks>Logs an error if the search model cannot be found.</remarks>
    /// <param name="webPageItem">The node to check for indexing.</param>
    /// <param name="indexName">The Lucene index code name.</param>
    /// <exception cref="ArgumentNullException" />
    public static bool IsIndexedByIndex(this IWebPageContentQueryDataContainer webPageItem, string indexName)
    {
        if (string.IsNullOrEmpty(indexName))
        {
            throw new ArgumentNullException(nameof(indexName));
        }
        if (webPageItem == null)
        {
            throw new ArgumentNullException(nameof(webPageItem));
        }

        var luceneIndex = IndexStore.Instance.GetIndex(indexName);
        if (luceneIndex == null)
        {
            Service.Resolve<IEventLogService>().LogError(nameof(IWebPageContentQueryDataContainerExtensions), nameof(IsIndexedByIndex), $"Error loading registered Lucene index '{indexName}.'");
            return false;
        }

        if (!luceneIndex.LuceneIndexingStrategy.ShouldIndexNode(webPageItem))
        {
            return false;
        }

        return luceneIndex.IncludedPaths.Any(includedPathAttribute =>
        {
            bool matchesContentType = includedPathAttribute.ContentTypes == null || includedPathAttribute.ContentTypes.Length == 0 || includedPathAttribute.ContentTypes.Contains(webPageItem.GetType().Name);
            if (includedPathAttribute.AliasPath.EndsWith("/%"))
            {
                string? pathToMatch = TreePathUtils.EnsureSingleNodePath(includedPathAttribute.AliasPath);
                var pathsOnPath = TreePathUtils.GetNodeAliasPathsOnPath(webPageItem.WebPageItemTreePath, true, false).ToHashSet();

                return pathsOnPath.Contains(pathToMatch) && matchesContentType;
            }
            else
            {
                return webPageItem.WebPageItemTreePath.Equals(includedPathAttribute.AliasPath, StringComparison.OrdinalIgnoreCase) && matchesContentType;
            }
        });
    }
}
