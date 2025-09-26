namespace Kentico.Xperience.Lucene.Core.Indexing;

/// <summary>
/// Extension methods used for configuring Lucene indexes.
/// </summary>
internal static class LuceneConfigurationExtensions
{
    /// <summary>
    /// Gets the alias path for the specified included path item ID from the Lucene index configuration.
    /// </summary>
    /// <param name="configuration">The <see cref="LuceneIndexModel"/>.</param>
    /// <param name="luceneIncludedPathItemId">Id of the path item.</param>
    /// <returns><see cref="string"/> containing the alias path.</returns>
    internal static string GetAliasPath(this LuceneIndexModel configuration, int? luceneIncludedPathItemId)
    => configuration.Channels
        .SelectMany(x => x.IncludedPaths).Single(x => x.Identifier == luceneIncludedPathItemId).AliasPath;


    /// <summary>
    /// Gets the content types configured for a specific path in the new channel configurations that do not exist in the existing content types.
    /// </summary>
    /// <param name="newChannelConfigurations">The new <see cref="IEnumerable{LuceneIndexChannelConfiguration}"/>.</param>
    /// <param name="path">The <see cref="LuceneIncludedPathItemInfo"/> in a new channel configuration.</param>
    /// <param name="existingContentTypes"><see cref="IEnumerable{LuceneContentTypeItemInfo}"/> containing already stored items.</param>
    /// <returns><see cref="IEnumerable{LuceneIndexContentType}"/> which are not stored.</returns>
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
    /// <param name="existingContentTypes"><see cref="IEnumerable{LuceneIndexChannelConfiguration}"/> containing already stored items.</param>
    /// <param name="newChannelConfigurations">The new <see cref="IEnumerable{LuceneIndexChannelConfiguration}"/>.</param>
    /// <returns>Already stored item IDs.</returns>
    internal static IEnumerable<int> GetIdsOfExistingContentTypesNotInNewChannelConfigurations(this IEnumerable<LuceneContentTypeItemInfo> existingContentTypes, IEnumerable<LuceneIndexChannelConfiguration> newChannelConfigurations)
    => existingContentTypes
        .Where(x => !newChannelConfigurations.SelectMany(x => x.IncludedPaths)
            .Any(y => y.ContentTypes
                .Exists(z => x.LuceneContentTypeItemIncludedPathItemId == y.Identifier && string.Equals(x.LuceneContentTypeItemContentTypeName, z.ContentTypeName)))
        )
        .Select(x => x.LuceneContentTypeItemId);


    /// <summary>
    /// Get the IDs of all included paths from the channel configurations.
    /// </summary>
    /// <param name="channelConfigurations">The <see cref="IEnumerable{LuceneIndexChannelConfiguration}"/>.</param>
    /// <returns>Array of IDs of all included paths.</returns>
    internal static int[] GetIdsOfAllIncludedPaths(this IEnumerable<LuceneIndexChannelConfiguration> channelConfigurations)
        => channelConfigurations.SelectMany(x => x.IncludedPaths).Select(x => x.Identifier ?? 0).ToArray();
}
