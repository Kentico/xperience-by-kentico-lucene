using Kentico.Xperience.Lucene.Admin;
using Kentico.Xperience.Lucene.Indexing;
using Kentico.Xperience.Lucene.Tests.Base;

namespace Kentico.Xperience.Lucene.Tests.Tests;
internal class IndexStoreTests
{

    [Test]
    public void AddAndGetIndex()
    {
        LuceneIndexStore.Instance.SetIndicies(new List<LuceneConfigurationModel>());

        LuceneIndexStore.Instance.AddIndex(MockDataProvider.Index);
        LuceneIndexStore.Instance.AddIndex(MockDataProvider.GetIndex("TestIndex", 1));

        Assert.Multiple(() =>
        {
            Assert.That(LuceneIndexStore.Instance.GetIndex("TestIndex") is not null);
            Assert.That(LuceneIndexStore.Instance.GetIndex(MockDataProvider.DefaultIndex) is not null);
        });
    }

    [Test]
    public void AddIndex_AlreadyExists()
    {
        LuceneIndexStore.Instance.SetIndicies(new List<LuceneConfigurationModel>());
        LuceneIndexStore.Instance.AddIndex(MockDataProvider.Index);

        bool hasThrown = false;

        try
        {
            LuceneIndexStore.Instance.AddIndex(MockDataProvider.Index);
        }
        catch
        {
            hasThrown = true;
        }

        Assert.That(hasThrown);
    }

    [Test]
    public void SetIndicies()
    {
        var defaultIndex = new LuceneConfigurationModel { IndexName = "DefaultIndex", Id = 0 };
        var simpleIndex = new LuceneConfigurationModel { IndexName = "SimpleIndex", Id = 1 };

        LuceneIndexStore.Instance.SetIndicies(new List<LuceneConfigurationModel>() { defaultIndex, simpleIndex });

        Assert.Multiple(() =>
        {
            Assert.That(LuceneIndexStore.Instance.GetIndex(defaultIndex.IndexName) is not null);
            Assert.That(LuceneIndexStore.Instance.GetIndex(simpleIndex.IndexName) is not null);
        });
    }
}
