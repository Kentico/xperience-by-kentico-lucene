using CMS.Core;
using DancingGoat.Models;
using Kentico.Xperience.Lucene.Admin;
using Kentico.Xperience.Lucene.Indexing;
using Kentico.Xperience.Lucene.Tests.Base;
namespace Kentico.Xperience.Lucene.Tests.Tests;

internal class MockEventLogService : IEventLogService
{
    public void LogEvent(EventLogData eventLogData) { }
}

internal class IndexedItemModelExtensionsTests
{
    private readonly IEventLogService log;

    public IndexedItemModelExtensionsTests() => log = new MockEventLogService();

    [Test]
    public void IsIndexedByIndex()
    {
        LuceneIndexStore.Instance.ClearIndexes();
        LuceneIndexStore.Instance.AddIndex(MockDataProvider.Index);

        Assert.That(MockDataProvider.WebModel.IsIndexedByIndex(log, MockDataProvider.DefaultIndex, MockDataProvider.EventName));
    }

    [Test]
    public void WildCard()
    {
        var model = MockDataProvider.WebModel;
        model.WebPageItemTreePath = "/Home";

        var index = MockDataProvider.Index;
        var path = new LuceneIndexIncludedPath("/%") { ContentTypes = new[] { ArticlePage.CONTENT_TYPE_NAME } };

        index.IncludedPaths = new List<LuceneIndexIncludedPath>() { path };

        LuceneIndexStore.Instance.ClearIndexes();
        LuceneIndexStore.Instance.AddIndex(index);

        Assert.True(model.IsIndexedByIndex(log, MockDataProvider.DefaultIndex, MockDataProvider.EventName));
    }

    [Test]
    public void WrongWildCard()
    {
        var model = MockDataProvider.WebModel;
        model.WebPageItemTreePath = "/Home";

        var index = MockDataProvider.Index;
        var path = new LuceneIndexIncludedPath("/Index/%") { ContentTypes = new[] { ArticlePage.CONTENT_TYPE_NAME } };

        index.IncludedPaths = new List<LuceneIndexIncludedPath>() { path };

        LuceneIndexStore.Instance.ClearIndexes();
        LuceneIndexStore.Instance.AddIndex(index);

        Assert.False(model.IsIndexedByIndex(log, MockDataProvider.DefaultIndex, MockDataProvider.EventName));
    }

    [Test]
    public void WrongPath()
    {
        var model = MockDataProvider.WebModel;
        model.WebPageItemTreePath = "/Home";

        var index = MockDataProvider.Index;
        var path = new LuceneIndexIncludedPath("/Index") { ContentTypes = new[] { ArticlePage.CONTENT_TYPE_NAME } };

        index.IncludedPaths = new List<LuceneIndexIncludedPath>() { path };

        LuceneIndexStore.Instance.ClearIndexes();
        LuceneIndexStore.Instance.AddIndex(index);

        Assert.False(model.IsIndexedByIndex(log, MockDataProvider.DefaultIndex, MockDataProvider.EventName));
    }

    [Test]
    public void WrongContentType()
    {
        var model = MockDataProvider.WebModel;
        model.ContentTypeName = "DancingGoat.HomePage";

        LuceneIndexStore.Instance.ClearIndexes();
        LuceneIndexStore.Instance.AddIndex(MockDataProvider.Index);

        Assert.False(model.IsIndexedByIndex(log, MockDataProvider.DefaultIndex, MockDataProvider.EventName));
    }

    [Test]
    public void WrongIndex()
    {
        LuceneIndexStore.Instance.ClearIndexes();
        LuceneIndexStore.Instance.AddIndex(MockDataProvider.Index);

        Assert.False(MockDataProvider.WebModel.IsIndexedByIndex(log, "NewIndex", MockDataProvider.EventName));
    }

    [Test]
    public void WrongLanguage()
    {
        var model = MockDataProvider.WebModel;
        model.LanguageName = "sk";

        LuceneIndexStore.Instance.ClearIndexes();
        LuceneIndexStore.Instance.AddIndex(MockDataProvider.Index);

        Assert.False(model.IsIndexedByIndex(log, MockDataProvider.DefaultIndex, MockDataProvider.EventName));
    }
}
