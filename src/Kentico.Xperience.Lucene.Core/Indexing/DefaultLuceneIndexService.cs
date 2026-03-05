using CMS.Helpers.Synchronization;

using Kentico.Xperience.Lucene.Core.Store;

using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Facet.Taxonomy.Directory;
using Lucene.Net.Index;

using Microsoft.Extensions.Hosting;

using LuceneDirectory = Lucene.Net.Store.Directory;

namespace Kentico.Xperience.Lucene.Core.Indexing;

public class DefaultLuceneIndexService(IHostEnvironment hostEnvironment) : ILuceneIndexService
{
    private readonly IHostEnvironment hostEnvironment = hostEnvironment;


    public T UseIndexAndTaxonomyWriter<T>(LuceneIndex index, Func<IndexWriter, ITaxonomyWriter, T> useIndexWriter, IndexStorageModel storage, OpenMode openMode = OpenMode.CREATE_OR_APPEND)
    {
        var lockAcquired = false;
        var fileLock = new FileLock(LuceneIndexLockHelper.GetLockFilePath(storage.Path, hostEnvironment));

        try
        {
            var analyzer = index.LuceneAnalyzer;

            //Create an index writer
            var indexConfig = new IndexWriterConfig(AnalyzerStorage.AnalyzerLuceneVersion, analyzer)
            {
                OpenMode = openMode // create/overwrite index
            };

            lockAcquired = fileLock.WaitForLock(LuceneIndexLockHelper.LOCK_WAIT_TIMEOUT);

            using LuceneDirectory indexDir = CmsIODirectory.Open(storage.Path);
            using var writer = new IndexWriter(indexDir, indexConfig);
            using LuceneDirectory taxonomyDir = CmsIODirectory.Open(storage.TaxonomyPath);
            using var taxonomyWriter = new DirectoryTaxonomyWriter(taxonomyDir);
            return useIndexWriter(writer, taxonomyWriter);

        }
        finally
        {
            if (lockAcquired)
            {
                fileLock.Release();
            }
        }
    }


    public TResult UseWriter<TResult>(LuceneIndex index, Func<IndexWriter, TResult> useIndexWriter, IndexStorageModel storage, OpenMode openMode = OpenMode.CREATE_OR_APPEND)
    {
        var lockAcquired = false;
        var fileLock = new FileLock(LuceneIndexLockHelper.GetLockFilePath(storage.Path, hostEnvironment));

        try
        {
            var analyzer = index.LuceneAnalyzer;

            //Create an index writer
            var indexConfig = new IndexWriterConfig(AnalyzerStorage.AnalyzerLuceneVersion, analyzer)
            {
                OpenMode = openMode // create/overwrite index
            };

            lockAcquired = fileLock.WaitForLock(LuceneIndexLockHelper.LOCK_WAIT_TIMEOUT);

            using LuceneDirectory indexDir = CmsIODirectory.Open(storage.Path);
            using var writer = new IndexWriter(indexDir, indexConfig);
            return useIndexWriter(writer);
        }
        finally
        {
            if (lockAcquired)
            {
                fileLock.Release();
            }
        }
    }


    public void ResetIndex(LuceneIndex index)
    {
        index.StorageContext.EnforceRetentionPolicy();
        UseWriter(index, (IndexWriter writer) => true, index.StorageContext.GetNextGeneration(), OpenMode.CREATE);
    }
}
