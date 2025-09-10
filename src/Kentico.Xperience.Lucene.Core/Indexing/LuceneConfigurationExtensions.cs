namespace Kentico.Xperience.Lucene.Core.Indexing;

/// <summary>
/// Extension methods used for configuring Lucene indexes.
/// </summary>
internal static class LuceneConfigurationExtensions
{
    /// <summary>
    /// Gets the alias path for the specified included path item ID from the Lucene index configuration.
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="luceneIncludedPathItemId"></param>
    /// <returns></returns>
    internal static string GetAliasPath(this LuceneIndexModel configuration, int? luceneIncludedPathItemId)
    => configuration.Channels
        .SelectMany(x => x.IncludedPaths).Single(x => x.Identifier == luceneIncludedPathItemId).AliasPath;


    /// <summary>
    /// Gets the content types configured for a specific path in the new channel configurations that do not exist in the existing content types.
    /// </summary>
    /// <param name="newChannelConfigurations"></param>
    /// <param name="path"></param>
    /// <param name="existingContentTypes"></param>
    /// <returns></returns>
    internal static IEnumerable<LuceneIndexContentType> GetContentTypesOfAPathConfigurationNotInExisting(this IEnumerable<LuceneIndexChannelConfiguration> newChannelConfigurations, LuceneIncludedPathItemInfo path, IEnumerable<LuceneContentTypeItemInfo> existingContentTypes)
    => newChannelConfigurations
        .SelectMany(x => x.IncludedPaths)
        .Single(x => x.Identifier == path.LuceneIncludedPathItemId)
        .ContentTypes
        .Where(x => !existingContentTypes
            .Any(y => y.LuceneContentTypeItemContentTypeName == x.ContentTypeName && y.LuceneContentTypeItemIncludedPathItemId == path.LuceneIncludedPathItemId)
        );


    /// <summary>
    /// Gets the IDs of existing content types that are not present in the new channel configurations.
    /// </summary>
    /// <param name="existingContentTypes"></param>
    /// <param name="newChannelConfigurations"></param>
    /// <returns></returns>
    internal static IEnumerable<int> GetIdsOfExistingContentTypesNotInNewChannelConfigurations(this IEnumerable<LuceneContentTypeItemInfo> existingContentTypes, IEnumerable<LuceneIndexChannelConfiguration> newChannelConfigurations)
    => existingContentTypes
        .Where(x => !newChannelConfigurations.SelectMany(x => x.IncludedPaths)
            .Any(y => y.ContentTypes
                .Exists(z => x.LuceneContentTypeItemIncludedPathItemId == y.Identifier && string.Equals(x.LuceneContentTypeItemContentTypeName, z.ContentTypeName)))
        )
        .Select(x => x.LuceneContentTypeItemId);
}
