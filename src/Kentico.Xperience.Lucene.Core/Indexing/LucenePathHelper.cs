namespace Kentico.Xperience.Lucene.Core.Indexing;

/// <summary>
/// Helper methods for working with Lucene index paths.
/// </summary>
internal static class LucenePathHelper
{
    /// <summary>
    /// Determines whether the specified path ends with a wildcard ("/%").
    /// </summary>
    /// <param name="path">The path.</param>
    internal static bool EndsWithWildcard(string path) => path.EndsWith("/%", StringComparison.OrdinalIgnoreCase);
}
