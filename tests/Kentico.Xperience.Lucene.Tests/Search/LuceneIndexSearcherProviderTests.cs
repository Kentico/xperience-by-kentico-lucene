using System.Collections.Concurrent;

using Kentico.Xperience.Lucene.Core.Indexing;
using Kentico.Xperience.Lucene.Core.Search;

using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;

using LuceneDirectory = Lucene.Net.Store.Directory;

namespace Kentico.Xperience.Lucene.Tests.Search;

/// <summary>
/// Tests the caching, lease ref-counting and invalidation lifecycle of <see cref="LuceneIndexSearcherProvider"/>
/// and <see cref="CachedIndex"/> using real Lucene readers over in-memory directories (no CMS.IO / disk).
/// </summary>
[TestFixture]
public class LuceneIndexSearcherProviderTests
{
    // ---- CachedIndex lifecycle ----

    [Test]
    public void TryAcquire_ReturnsLeaseWithUsableSearcher()
    {
        var dir = CreateCommittedIndex();
        var cached = NewCachedIndex(dir);

        using var lease = cached.TryAcquire(withTaxonomy: false);

        Assert.That(lease, Is.Not.Null);
        Assert.That(lease!.Searcher, Is.Not.Null);
        Assert.That(lease.TaxonomyReader, Is.Null);
    }


    [Test]
    public void TryAcquire_MultipleLeases_ShareTheSameSearcher()
    {
        var dir = CreateCommittedIndex();
        var cached = NewCachedIndex(dir);

        using var first = cached.TryAcquire(withTaxonomy: false);
        using var second = cached.TryAcquire(withTaxonomy: false);

        Assert.That(second!.Searcher, Is.SameAs(first!.Searcher));
    }


    [Test]
    public void Retire_WithNoLeases_DisposesImmediately()
    {
        var dir = CreateCommittedIndex();
        var cached = NewCachedIndex(dir);

        cached.Retire();

        Assert.That(dir.IsDisposed, Is.True);
    }


    [Test]
    public void Retire_WithActiveLease_DefersDisposalUntilReleased()
    {
        var dir = CreateCommittedIndex();
        var cached = NewCachedIndex(dir);
        var lease = cached.TryAcquire(withTaxonomy: false);

        cached.Retire();
        Assert.That(dir.IsDisposed, Is.False, "the directory must stay open while a lease is held");

        lease!.Dispose();
        Assert.That(dir.IsDisposed, Is.True, "the directory should be disposed once the last lease is released");
    }


    [Test]
    public void TryAcquire_AfterRetire_ReturnsNull()
    {
        var dir = CreateCommittedIndex();
        var cached = NewCachedIndex(dir);

        cached.Retire();

        Assert.That(cached.TryAcquire(withTaxonomy: false), Is.Null);
    }


    [Test]
    public void TryAcquire_WithTaxonomyButNoFactory_ThrowsAndReleasesLease()
    {
        var dir = CreateCommittedIndex();
        var cached = NewCachedIndex(dir);

        Assert.Throws<InvalidOperationException>(() => cached.TryAcquire(withTaxonomy: true));

        // The failed acquisition must not leak a lease - retiring with no outstanding leases disposes now.
        cached.Retire();
        Assert.That(dir.IsDisposed, Is.True);
    }


    // ---- Empty (nothing published) fallback ----

    [Test]
    public void OpenEmpty_ServesSearchableEmptyIndex()
    {
        AnalyzerStorage.SetAnalyzerLuceneVersion(LuceneVersion.LUCENE_48);
        using var analyzer = new StandardAnalyzer(LuceneVersion.LUCENE_48);

        var cached = CachedIndex.OpenEmpty(analyzer);

        using (var lease = cached.TryAcquire(withTaxonomy: false))
        {
            Assert.That(lease, Is.Not.Null);
            Assert.That(lease!.Searcher.IndexReader.NumDocs, Is.EqualTo(0));
        }

        cached.Retire();
    }


    [Test]
    public void OpenEmpty_SupportsFacetedAcquisition()
    {
        AnalyzerStorage.SetAnalyzerLuceneVersion(LuceneVersion.LUCENE_48);
        using var analyzer = new StandardAnalyzer(LuceneVersion.LUCENE_48);

        var cached = CachedIndex.OpenEmpty(analyzer);

        // A faceted search while nothing is published must not fall through to the on-disk taxonomy folder
        // of the generation being rebuilt - it gets an empty in-memory taxonomy reader instead.
        using (var lease = cached.TryAcquire(withTaxonomy: true))
        {
            Assert.That(lease, Is.Not.Null);
            Assert.That(lease!.TaxonomyReader, Is.Not.Null);
        }

        cached.Retire();
    }


    // ---- Provider caching / invalidation ----

    [Test]
    public void AcquireInternal_CacheHit_DoesNotReopen()
    {
        using var provider = new LuceneIndexSearcherProvider(Substitute.For<ILuceneIndexService>());
        var dir = CreateCommittedIndex();
        int opens = 0;
        CachedIndex Factory()
        {
            opens++;
            return NewCachedIndex(dir);
        }

        using var first = provider.AcquireInternal("idx", Factory, withTaxonomy: false);
        using var second = provider.AcquireInternal("idx", Factory, withTaxonomy: false);

        Assert.That(opens, Is.EqualTo(1), "a cache hit must not open a new reader");
        Assert.That(second.Searcher, Is.SameAs(first.Searcher));
    }


    [Test]
    public void Invalidate_RebuildsOnNextAcquire_AndDisposesOldGeneration()
    {
        using var provider = new LuceneIndexSearcherProvider(Substitute.For<ILuceneIndexService>());
        var dir1 = CreateCommittedIndex();
        var dir2 = CreateCommittedIndex();
        int opens = 0;
        CachedIndex Factory()
        {
            opens++;
            return NewCachedIndex(opens == 1 ? dir1 : dir2);
        }

        provider.AcquireInternal("idx", Factory, withTaxonomy: false).Dispose();

        provider.Invalidate("idx");
        Assert.That(dir1.IsDisposed, Is.True, "the old generation should be disposed once no leases remain");

        using var rebuilt = provider.AcquireInternal("idx", Factory, withTaxonomy: false);
        Assert.That(opens, Is.EqualTo(2), "a new generation should be opened after invalidation");
    }


    [Test]
    public void Invalidate_WithActiveLease_DefersOldGenerationDisposal()
    {
        using var provider = new LuceneIndexSearcherProvider(Substitute.For<ILuceneIndexService>());
        var dir = CreateCommittedIndex();
        var lease = provider.AcquireInternal("idx", () => NewCachedIndex(dir), withTaxonomy: false);

        provider.Invalidate("idx");
        Assert.That(dir.IsDisposed, Is.False, "an in-flight search must not have its reader disposed");

        lease.Dispose();
        Assert.That(dir.IsDisposed, Is.True);
    }


    // ---- Waiting for readers to drain before a rename ----

    [Test]
    public void InvalidateAndWait_NoActiveLease_ReturnsImmediately()
    {
        using var provider = new LuceneIndexSearcherProvider(Substitute.For<ILuceneIndexService>());
        var dir = CreateCommittedIndex();

        provider.AcquireInternal("idx", () => NewCachedIndex(dir), withTaxonomy: false).Dispose();

        Assert.That(provider.InvalidateAndWait("idx", TimeSpan.FromSeconds(5)), Is.True);
        Assert.That(dir.IsDisposed, Is.True);
    }


    [Test]
    public void InvalidateAndWait_UncachedIndex_ReturnsTrue()
    {
        using var provider = new LuceneIndexSearcherProvider(Substitute.For<ILuceneIndexService>());

        Assert.That(provider.InvalidateAndWait("never-opened", TimeSpan.Zero), Is.True);
    }


    [Test]
    public void InvalidateAndWait_LeaseHeldPastTimeout_ReturnsFalse()
    {
        using var provider = new LuceneIndexSearcherProvider(Substitute.For<ILuceneIndexService>());
        var dir = CreateCommittedIndex();

        // The lease is deliberately never released - the caller must not block indefinitely behind it.
        var lease = provider.AcquireInternal("idx", () => NewCachedIndex(dir), withTaxonomy: false);

        Assert.That(provider.InvalidateAndWait("idx", TimeSpan.FromMilliseconds(100)), Is.False);
        Assert.That(dir.IsDisposed, Is.False);

        lease.Dispose();
    }


    [Test]
    public void InvalidateAndWait_LeaseReleasedConcurrently_UnblocksAndReportsRelease()
    {
        using var provider = new LuceneIndexSearcherProvider(Substitute.For<ILuceneIndexService>());
        var dir = CreateCommittedIndex();
        var lease = provider.AcquireInternal("idx", () => NewCachedIndex(dir), withTaxonomy: false);

        // Mimics an in-flight search finishing shortly after a publish begins: the wait must return as soon
        // as the handles are actually gone, not after a fixed sleep.
        var releaser = Task.Run(async () =>
        {
            await Task.Delay(150);
            lease.Dispose();
        });

        bool released = provider.InvalidateAndWait("idx", TimeSpan.FromSeconds(10));

        Assert.Multiple(() =>
        {
            Assert.That(released, Is.True);
            Assert.That(dir.IsDisposed, Is.True, "the wait must not return before the reader is disposed");
        });

        releaser.GetAwaiter().GetResult();
    }


    [Test]
    public void Dispose_RetiresAllCachedIndexes()
    {
        var provider = new LuceneIndexSearcherProvider(Substitute.For<ILuceneIndexService>());
        var dir = CreateCommittedIndex();

        provider.AcquireInternal("idx", () => NewCachedIndex(dir), withTaxonomy: false).Dispose();
        provider.Dispose();

        Assert.That(dir.IsDisposed, Is.True);
    }


    [Test]
    public void AcquireInternal_AfterDispose_Throws()
    {
        var provider = new LuceneIndexSearcherProvider(Substitute.For<ILuceneIndexService>());
        provider.Dispose();

        Assert.Throws<ObjectDisposedException>(
            () => provider.AcquireInternal("idx", () => NewCachedIndex(CreateCommittedIndex()), withTaxonomy: false));
    }


    [Test]
    public async Task ConcurrentAcquireReleaseWithInvalidate_NeverThrows_AndDisposesEveryGeneration()
    {
        using var provider = new LuceneIndexSearcherProvider(Substitute.For<ILuceneIndexService>());
        var openedDirs = new ConcurrentBag<TrackingRamDirectory>();

        CachedIndex Factory()
        {
            var dir = CreateCommittedIndex();
            openedDirs.Add(dir);
            return NewCachedIndex(dir);
        }

        var exceptions = new ConcurrentBag<Exception>();
        var tasks = new List<Task>();

        for (int t = 0; t < 8; t++)
        {
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    for (int i = 0; i < 200; i++)
                    {
                        using var lease = provider.AcquireInternal("idx", Factory, withTaxonomy: false);
                        // Touch the reader to ensure it is not disposed while in use.
                        _ = lease.Searcher.IndexReader.NumDocs;
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }));
        }

        tasks.Add(Task.Run(() =>
        {
            for (int i = 0; i < 50; i++)
            {
                provider.Invalidate("idx");
                Thread.Yield();
            }
        }));

        await Task.WhenAll(tasks);

        Assert.That(exceptions, Is.Empty, "no search should observe a disposed reader");

        provider.Dispose();
        Assert.That(openedDirs, Is.Not.Empty);
        Assert.That(openedDirs, Has.All.Matches<TrackingRamDirectory>(d => d.IsDisposed),
            "every opened generation should be disposed once retired and drained");
    }


    // ---- Helpers ----

    private static CachedIndex NewCachedIndex(LuceneDirectory dir) =>
        new(dir, new SearcherManager(dir, null), openTaxonomy: null);


    /// <summary>Creates a tracking in-memory directory containing a single committed document.</summary>
    private static TrackingRamDirectory CreateCommittedIndex()
    {
        var dir = new TrackingRamDirectory();
        using var analyzer = new StandardAnalyzer(LuceneVersion.LUCENE_48);
        var config = new IndexWriterConfig(LuceneVersion.LUCENE_48, analyzer);
        using var writer = new IndexWriter(dir, config);

        var document = new Document
        {
            new TextField("content", "hello world", Field.Store.YES)
        };
        writer.AddDocument(document);
        writer.Commit();

        return dir;
    }


    /// <summary>A <see cref="RAMDirectory"/> that records whether it has been disposed.</summary>
    private sealed class TrackingRamDirectory : RAMDirectory
    {
        public bool IsDisposed { get; private set; }

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            base.Dispose(disposing);
        }
    }
}
