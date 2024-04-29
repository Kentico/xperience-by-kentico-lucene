using CMS.DataEngine;

using DancingGoat.Models;

using Kentico.Xperience.Lucene.Core.Indexing;

using Lucene.Net.Util;

namespace Kentico.Xperience.Lucene.Tests.Base;

internal static class MockDataProvider
{
    public static IndexEventWebPageItemModel WebModel => new(
        itemID: 0,
        itemGuid: Guid.NewGuid(),
        languageName: CzechLanguageName,
        contentTypeName: ArticlePage.CONTENT_TYPE_NAME,
        name: "Name",
        isSecured: false,
        contentTypeID: 1,
        contentLanguageID: 1,
        websiteChannelName: DefaultChannel,
        webPageItemTreePath: "/",
        order: 0
    );

    public static LuceneIndexIncludedPath Path => new("/%")
    {
        ContentTypes = [new LuceneIndexContentType(ArticlePage.CONTENT_TYPE_NAME,
            DataClassInfoProvider.ProviderObject
                .Get()
                .WhereEquals(nameof(DataClassInfo.ClassName), ArticlePage.CONTENT_TYPE_NAME)
                .FirstOrDefault()?
                .ClassDisplayName ?? "", 0
                )
            ]
    };


    public static LuceneIndex Index => new(
        new LuceneIndexModel()
        {
            IndexName = DefaultIndex,
            ChannelName = DefaultChannel,
            LanguageNames = [EnglishLanguageName, CzechLanguageName],
            Paths = [Path],
            AnalyzerName = DefaultAnalyzer
        },
        [],
        [],
        LuceneVersion.LUCENE_48
    );

    public static readonly string DefaultIndex = "SimpleIndex";
    public static readonly string DefaultChannel = "DefaultChannel";
    public static readonly string DefaultAnalyzer = "StandardAnalyzer";
    public static readonly string EnglishLanguageName = "en";
    public static readonly string CzechLanguageName = "cz";
    public static readonly int IndexId = 1;
    public static readonly string EventName = "publish";

    public static LuceneIndex GetIndex(string indexName, int id) => new(
        new LuceneIndexModel()
        {
            Id = id,
            IndexName = indexName,
            ChannelName = DefaultChannel,
            LanguageNames = [EnglishLanguageName, CzechLanguageName],
            Paths = [Path],
        },
        [],
        [],
        LuceneVersion.LUCENE_48
    );
}
