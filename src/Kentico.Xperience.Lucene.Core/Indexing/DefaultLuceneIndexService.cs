using Kentico.Xperience.Lucene.Core.Store;

using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Facet.Taxonomy.Directory;
using Lucene.Net.Index;

using LuceneDirectory = Lucene.Net.Store.Directory;

namespace Kentico.Xperience.Lucene.Core.Indexing;

public class DefaultLuceneIndexService : ILuceneIndexService
{
    private const int WRITE_LOCK_TIMEOUT_IN_MS = 1_000_000_000;


    public T UseIndexAndTaxonomyWriter<T>(LuceneIndex index, Func<IndexWriter, ITaxonomyWriter, T> useIndexWriter, IndexStorageModel storage, OpenMode openMode = OpenMode.CREATE_OR_APPEND)
    {
        using LuceneDirectory indexDir = CmsIODirectory.Open(storage.Path);
        var analyzer = index.LuceneAnalyzer;

        //Create an index writer
        var indexConfig = new IndexWriterConfig(AnalyzerStorage.AnalyzerLuceneVersion, analyzer)
        {
            OpenMode = openMode, // create/overwrite index
            WriteLockTimeout = WRITE_LOCK_TIMEOUT_IN_MS
        };

        using var writer = new IndexWriter(indexDir, indexConfig);
        using LuceneDirectory taxonomyDir = CmsIODirectory.Open(storage.TaxonomyPath);
        using var taxonomyWriter = new DirectoryTaxonomyWriter(taxonomyDir);
        return useIndexWriter(writer, taxonomyWriter);
    }


    public TResult UseWriter<TResult>(LuceneIndex index, Func<IndexWriter, TResult> useIndexWriter, IndexStorageModel storage, OpenMode openMode = OpenMode.CREATE_OR_APPEND)
    {
        using LuceneDirectory indexDir = CmsIODirectory.Open(storage.Path);
        var analyzer = index.LuceneAnalyzer;

        //Create an index writer
        var indexConfig = new IndexWriterConfig(AnalyzerStorage.AnalyzerLuceneVersion, analyzer)
        {
            OpenMode = openMode, // create/overwrite index
            WriteLockTimeout = WRITE_LOCK_TIMEOUT_IN_MS
        };
        using var writer = new IndexWriter(indexDir, indexConfig);
        return useIndexWriter(writer);
    }


    public void ResetIndex(LuceneIndex index)
    {
        index.StorageContext.EnforceRetentionPolicy();
        UseWriter(index, (IndexWriter writer) => true, index.StorageContext.GetNextGeneration(), OpenMode.CREATE);
    }
}
