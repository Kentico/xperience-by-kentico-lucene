using Kentico.Xperience.Lucene.Admin;
using Kentico.Xperience.Lucene.Indexing;
using Kentico.Xperience.Lucene.Tests.Base;

namespace Kentico.Xperience.Lucene.Tests.Tests;
internal class IndexStoreTests
{

    [Test]
    public void AddAndGetIndex()
    {
        LuceneIndexStore.Instance.ClearIndexes();

        LuceneIndexStore.Instance.AddIndex(MockDataProvider.Index);
        LuceneIndexStore.Instance.AddIndex(MockDataProvider.GetIndex("TestIndex"));

        Assert.Multiple(() =>
        {
            Assert.IsNotNull(LuceneIndexStore.Instance.GetIndex("TestIndex"));
            Assert.IsNotNull(LuceneIndexStore.Instance.GetIndex(MockDataProvider.DefaultIndex));
        });
    }

    [Test]
    public void AddIndex_AlreadyExists()
    {
        LuceneIndexStore.Instance.ClearIndexes();
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

        Assert.IsTrue(hasThrown);
    }

    [Test]
    public void AddIndices()
    {
        var defaultIndex = new LuceneConfigurationModel { IndexName = "DefaultIndex" };
        var simpleIndex = new LuceneConfigurationModel { IndexName = "SimpleIndex" };

        LuceneIndexStore.Instance.AddIndices(new List<LuceneConfigurationModel>() { defaultIndex, simpleIndex });

        Assert.Multiple(() =>
        {
            Assert.IsNotNull(LuceneIndexStore.Instance.GetIndex(defaultIndex.IndexName));
            Assert.IsNotNull(LuceneIndexStore.Instance.GetIndex(simpleIndex.IndexName));
        });
    }
}
