using DancingGoat.Models;
using Kentico.Xperience.Lucene.Admin;
using Kentico.Xperience.Lucene.Indexing;

namespace Kentico.Xperience.Lucene.Tests.Base;
internal static class MockDataProvider
{
    public static IndexEventWebPageItemModel WebModel => new(
        itemID: 0,
        itemGuid: new Guid(),
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
        ContentTypes = [ArticlePage.CONTENT_TYPE_NAME]
    };


    public static LuceneIndex Index => new(
        new LuceneConfigurationModel()
        {
            IndexName = DefaultIndex,
            ChannelName = DefaultChannel,
            LanguageNames = new List<string>() { EnglishLanguageName, CzechLanguageName },
            Paths = new List<LuceneIndexIncludedPath>() { Path }
        },
        []
    );

    public static readonly string DefaultIndex = "SimpleIndex";
    public static readonly string DefaultChannel = "DefaultChannel";
    public static readonly string EnglishLanguageName = "en";
    public static readonly string CzechLanguageName = "cz";
    public static readonly int IndexId = 1;
    public static readonly string EventName = "publish";

    public static LuceneIndex GetIndex(string indexName, int id) => new(
        new LuceneConfigurationModel()
        {
            Id = id,
            IndexName = indexName,
            ChannelName = DefaultChannel,
            LanguageNames = new List<string>() { EnglishLanguageName, CzechLanguageName },
            Paths = new List<LuceneIndexIncludedPath>() { Path }
        },
        []
    );
}
