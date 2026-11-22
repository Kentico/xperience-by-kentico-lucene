using CMS.Helpers;

using Kentico.Xperience.Lucene.Core.Indexing;

namespace Kentico.Xperience.Lucene.Tests.Indexing;

[TestFixture]
public class DefaultLuceneIndexManagerTests
{
    [Test]
    public void GetIndex_ByName_WhenIndexDoesNotExist_DisablesCaching()
    {
        var storageService = Substitute.For<ILuceneConfigurationStorageService>();
        storageService.GetIndexDataOrNullAsync("MissingIndex")
            .Returns(Task.FromResult<LuceneIndexModel?>(null));

        var (manager, getCacheSettings) = CreateManager(storageService);

        var result = manager.GetIndex("MissingIndex");

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Null);
            Assert.That(getCacheSettings()?.Cached, Is.False);
        });
    }


    [Test]
    public void GetIndex_ByIdentifier_WhenIndexDoesNotExist_DisablesCaching()
    {
        var storageService = Substitute.For<ILuceneConfigurationStorageService>();
        storageService.GetIndexDataOrNullAsync(42)
            .Returns(Task.FromResult<LuceneIndexModel?>(null));

        var (manager, getCacheSettings) = CreateManager(storageService);

        var result = manager.GetIndex(42);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Null);
            Assert.That(getCacheSettings()?.Cached, Is.False);
        });
    }


    private static (DefaultLuceneIndexManager Manager, Func<CacheSettings?> GetCacheSettings) CreateManager(
        ILuceneConfigurationStorageService storageService)
    {
        var progressiveCache = Substitute.For<IProgressiveCache>();
        CacheSettings? cacheSettings = null;
        progressiveCache.Load(Arg.Any<Func<CacheSettings, LuceneIndex?>>(), Arg.Any<CacheSettings>())
            .Returns(callInfo =>
            {
                cacheSettings = callInfo.Arg<CacheSettings>();
                var load = callInfo.Arg<Func<CacheSettings, LuceneIndex?>>();
                return load(cacheSettings);
            });

        return (new DefaultLuceneIndexManager(storageService, progressiveCache), () => cacheSettings);
    }
}
