using Kentico.Xperience.Lucene.Models;
using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Facet.Taxonomy.Directory;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using LuceneDirectory = Lucene.Net.Store.Directory;

namespace Kentico.Xperience.Lucene.Indexing;

public class DefaultLuceneIndexService : ILuceneIndexService
{
    private const LuceneVersion LUCENE_VERSION = LuceneVersion.LUCENE_48;

    public T UseIndexAndTaxonomyWriter<T>(LuceneIndex index, Func<IndexWriter, ITaxonomyWriter, T> useIndexWriter, IndexStorageModel storage, OpenMode openMode = OpenMode.CREATE_OR_APPEND)
    {
        using LuceneDirectory indexDir = FSDirectory.Open(storage.Path);

        //Create an index writer
        var indexConfig = new IndexWriterConfig(LUCENE_VERSION, index.Analyzer)
        {
            OpenMode = openMode // create/overwrite index
        };
        using var writer = new IndexWriter(indexDir, indexConfig);

        using LuceneDirectory taxonomyDir = FSDirectory.Open(storage.TaxonomyPath);
        using var taxonomyWriter = new DirectoryTaxonomyWriter(taxonomyDir);

        return useIndexWriter(writer, taxonomyWriter);
    }

    public TResult UseWriter<TResult>(LuceneIndex index, Func<IndexWriter, TResult> useIndexWriter, IndexStorageModel storage, OpenMode openMode = OpenMode.CREATE_OR_APPEND)
    {
        using LuceneDirectory indexDir = FSDirectory.Open(storage.Path);

        //Create an index writer
        var indexConfig = new IndexWriterConfig(LUCENE_VERSION, index.Analyzer)
        {
            OpenMode = openMode // create/overwrite index
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
