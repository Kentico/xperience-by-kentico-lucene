using CMS.Core;
using DancingGoat.Models;
using Kentico.Xperience.Lucene.Admin;
using Kentico.Xperience.Lucene.Indexing;
using Kentico.Xperience.Lucene.Tests.Base;
namespace Kentico.Xperience.Lucene.Tests.Tests;

internal class MockEventLogService : IEventLogService
{
    public void LogEvent(EventLogData eventLogData)
    {
        // Method intentionally left empty.
    }
}

internal class IndexedItemModelExtensionsTests
{
    private readonly IEventLogService log;

    public IndexedItemModelExtensionsTests() => log = new MockEventLogService();

    [Test]
    public void IsIndexedByIndex()
    {
        LuceneIndexStore.Instance.SetIndicies(new List<LuceneConfigurationModel>());
        LuceneIndexStore.Instance.AddIndex(MockDataProvider.Index);

        Assert.That(MockDataProvider.WebModel.IsIndexedByIndex(log, MockDataProvider.DefaultIndex, MockDataProvider.EventName));
    }

    [Test]
    public void WildCard()
    {
        var model = MockDataProvider.WebModel;
        model.WebPageItemTreePath = "/Home";

        var index = MockDataProvider.Index;
        var path = new LuceneIndexIncludedPath("/%") { ContentTypes = [ArticlePage.CONTENT_TYPE_NAME] };

        index.IncludedPaths = new List<LuceneIndexIncludedPath>() { path };

        LuceneIndexStore.Instance.SetIndicies(new List<LuceneConfigurationModel>());
        LuceneIndexStore.Instance.AddIndex(index);

        Assert.That(model.IsIndexedByIndex(log, MockDataProvider.DefaultIndex, MockDataProvider.EventName));
    }

    [Test]
    public void WrongWildCard()
    {
        var model = MockDataProvider.WebModel;
        model.WebPageItemTreePath = "/Home";

        var index = MockDataProvider.Index;
        var path = new LuceneIndexIncludedPath("/Index/%") { ContentTypes = [ArticlePage.CONTENT_TYPE_NAME] };

        index.IncludedPaths = new List<LuceneIndexIncludedPath>() { path };

        LuceneIndexStore.Instance.SetIndicies(new List<LuceneConfigurationModel>());
        LuceneIndexStore.Instance.AddIndex(index);

        Assert.That(!model.IsIndexedByIndex(log, MockDataProvider.DefaultIndex, MockDataProvider.EventName));
    }

    [Test]
    public void WrongPath()
    {
        var model = MockDataProvider.WebModel;
        model.WebPageItemTreePath = "/Home";

        var index = MockDataProvider.Index;
        var path = new LuceneIndexIncludedPath("/Index") { ContentTypes = [ArticlePage.CONTENT_TYPE_NAME] };

        index.IncludedPaths = new List<LuceneIndexIncludedPath>() { path };

        LuceneIndexStore.Instance.SetIndicies(new List<LuceneConfigurationModel>());
        LuceneIndexStore.Instance.AddIndex(index);

        Assert.That(!model.IsIndexedByIndex(log, MockDataProvider.DefaultIndex, MockDataProvider.EventName));
    }

    [Test]
    public void WrongContentType()
    {
        var model = MockDataProvider.WebModel;
        model.ContentTypeName = "DancingGoat.HomePage";

        LuceneIndexStore.Instance.SetIndicies(new List<LuceneConfigurationModel>());
        LuceneIndexStore.Instance.AddIndex(MockDataProvider.Index);

        Assert.That(!model.IsIndexedByIndex(log, MockDataProvider.DefaultIndex, MockDataProvider.EventName));
    }

    [Test]
    public void WrongIndex()
    {
        LuceneIndexStore.Instance.SetIndicies(new List<LuceneConfigurationModel>());
        LuceneIndexStore.Instance.AddIndex(MockDataProvider.Index);

        Assert.That(!MockDataProvider.WebModel.IsIndexedByIndex(log, "NewIndex", MockDataProvider.EventName));
    }

    [Test]
    public void WrongLanguage()
    {
        var model = MockDataProvider.WebModel;
        model.LanguageName = "sk";

        LuceneIndexStore.Instance.SetIndicies(new List<LuceneConfigurationModel>());
        LuceneIndexStore.Instance.AddIndex(MockDataProvider.Index);

        Assert.That(!model.IsIndexedByIndex(log, MockDataProvider.DefaultIndex, MockDataProvider.EventName));
    }
}
