using CMS.Core;
using CMS.EventLog;
using CMS.Tests;
using FluentAssertions;
using Kentico.Xperience.Lucene.Admin;
using Kentico.Xperience.Lucene.Indexing;

namespace Kentico.Xperience.Lucene.Tests.Indexing;

public class Tests : UnitTests
{
    [TestCase("")]
    [TestCase(null)]
    [TestCase("   ")]
    public void IsIndexedByIndex_Will_Throw_When_IndexName_Is_Is_Invalid(
        string? indexName
    )
    {
        var fixture = new Fixture();

        var log = Substitute.For<IEventLogService>();

        var item = fixture.Create<IndexEventWebPageItemModel>();

        var sut = () => item.IsIndexedByIndex(log, indexName!, "event");
        sut.Should().ThrowExactly<ArgumentNullException>();
    }

    [Test]
    public void IsIndexedByIndex_Will_Throw_When_Item_Is_Is_Invalid()
    {
        var log = Substitute.For<IEventLogService>();

        IndexEventWebPageItemModel item = null!;

        var sut = () => item.IsIndexedByIndex(log, "index", "event");
        sut.Should().ThrowExactly<ArgumentNullException>();
    }

    [Test]
    public void IsIndexedByIndex_Will_Return_False_When_The_Index_Doesnt_Exist()
    {
        var log = Substitute.For<EventLogService>();

        var fixture = new Fixture();
        var sut = fixture.Create<IndexEventWebPageItemModel>();

        sut.IsIndexedByIndex(log, "index", "event").Should().BeFalse();
    }

    [Test]
    public void IsIndexedByIndex_Will_Return_False_When_The_Matching_Index_Has_No_Matching_ContentTypes()
    {
        var log = Substitute.For<EventLogService>();

        IEnumerable<LuceneIndexIncludedPath> paths = [new("/path") { ContentTypes = ["contentType"], Identifier = "1" }];

        var index = new LuceneIndex(new LuceneConfigurationModel
        {
            ChannelName = "channel",
            Id = 2,
            IndexName = "index2",
            LanguageNames = ["en-US"],
            Paths = paths,
            RebuildHook = "/rebuild",
            StrategyName = "strategy"
        }, new() { { "strategy", typeof(DefaultLuceneIndexingStrategy) } });
        LuceneIndexStore.Instance.AddIndex(index);

        var fixture = new Fixture();
        var sut = fixture.Create<IndexEventWebPageItemModel>();
        sut.ContentTypeName = paths.First().ContentTypes[0] + "-abc";

        sut.IsIndexedByIndex(log, index.IndexName, "event").Should().BeFalse();
    }

    [Test]
    public void IsIndexedByIndex_Will_Return_False_When_The_Matching_Index_Has_No_Matching_Paths()
    {
        var log = Substitute.For<EventLogService>();
        List<string> contentTypes = ["contentType"];

        IEnumerable<LuceneIndexIncludedPath> exactPaths = [new("/path") { ContentTypes = contentTypes, Identifier = "1" }];

        var index1 = new LuceneIndex(new LuceneConfigurationModel
        {
            ChannelName = "channel",
            Id = 1,
            IndexName = "index1",
            LanguageNames = ["en-US"],
            Paths = exactPaths,
            RebuildHook = "/rebuild",
            StrategyName = "strategy"
        }, new() { { "strategy", typeof(DefaultLuceneIndexingStrategy) } });
        LuceneIndexStore.Instance.AddIndex(index1);

        IEnumerable<LuceneIndexIncludedPath> wildcardPaths = [new("/home/%") { ContentTypes = contentTypes, Identifier = "1" }];
        var index2 = new LuceneIndex(new LuceneConfigurationModel
        {
            ChannelName = "channel",
            Id = 2,
            IndexName = "index2",
            LanguageNames = ["en-US"],
            Paths = wildcardPaths,
            RebuildHook = "/rebuild",
            StrategyName = "strategy"
        }, new() { { "strategy", typeof(DefaultLuceneIndexingStrategy) } });
        LuceneIndexStore.Instance.AddIndex(index2);

        var fixture = new Fixture();
        var sut = fixture.Create<IndexEventWebPageItemModel>();
        sut.ContentTypeName = contentTypes[0];
        sut.WebPageItemTreePath = exactPaths.First().AliasPath + "/abc";

        sut.IsIndexedByIndex(log, index1.IndexName, "event").Should().BeFalse();
        sut.IsIndexedByIndex(log, index2.IndexName, "event").Should().BeFalse();
    }

    [Test]
    public void IsIndexedByIndex_Will_Return_True_When_The_Matching_Index_Has_An_Exact_Path_Match()
    {
        var log = Substitute.For<EventLogService>();
        List<string> contentTypes = ["contentType"];

        IEnumerable<LuceneIndexIncludedPath> exactPaths = [new("/path/abc/def") { ContentTypes = contentTypes, Identifier = "1" }];

        var index1 = new LuceneIndex(new LuceneConfigurationModel
        {
            ChannelName = "channel",
            Id = 1,
            IndexName = "index1",
            LanguageNames = ["en-US"],
            Paths = exactPaths,
            RebuildHook = "/rebuild",
            StrategyName = "strategy"
        }, new() { { "strategy", typeof(DefaultLuceneIndexingStrategy) } });
        LuceneIndexStore.Instance.AddIndex(index1);

        IEnumerable<LuceneIndexIncludedPath> wildcardPaths = [new("/path/%") { ContentTypes = ["contentType"], Identifier = "1" }];

        var index2 = new LuceneIndex(new LuceneConfigurationModel
        {
            ChannelName = "channel",
            Id = 2,
            IndexName = "index2",
            LanguageNames = ["en-US"],
            Paths = wildcardPaths,
            RebuildHook = "/rebuild",
            StrategyName = "strategy"
        }, new() { { "strategy", typeof(DefaultLuceneIndexingStrategy) } });
        LuceneIndexStore.Instance.AddIndex(index2);

        var fixture = new Fixture();
        var sut = fixture.Create<IndexEventWebPageItemModel>();
        sut.ContentTypeName = contentTypes[0];
        sut.WebPageItemTreePath = exactPaths.First().AliasPath;

        sut.IsIndexedByIndex(log, index1.IndexName, "event").Should().BeTrue();
        sut.IsIndexedByIndex(log, index2.IndexName, "event").Should().BeTrue();
    }

    [TearDown]
    public void TearDown() => LuceneIndexStore.Instance.SetIndicies([]);
}
