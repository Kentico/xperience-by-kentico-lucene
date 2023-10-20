using Kentico.Xperience.Lucene.Models;
using Lucene.Net.Facet;
using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Facet.Taxonomy.Directory;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using LuceneDirectory = Lucene.Net.Store.Directory;

namespace Kentico.Xperience.Lucene.Services.Implementations;

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

    public TResult UseSearcher<TResult>(LuceneIndex index, Func<IndexSearcher, TResult> useIndexSearcher)
    {
        var storage = index.StorageContext.GetPublishedIndex();
        if (!System.IO.Directory.Exists(storage.Path))
        {
            // ensure index
            UseWriter(index, (writer) =>
            {
                writer.Commit();
                return true;
            }, storage);
        }

        using LuceneDirectory indexDir = FSDirectory.Open(storage.Path);
        using var reader = DirectoryReader.Open(indexDir);
        var searcher = new IndexSearcher(reader);
        return useIndexSearcher(searcher);
    }

    public TResult UseSearcherWithFacets<TResult>(LuceneIndex index, Query query, int n, Func<IndexSearcher, MultiFacets, TResult> useIndexSearcher)
    {
        var storage = index.StorageContext.GetPublishedIndex();
        if (!System.IO.Directory.Exists(storage.Path))
        {
            // ensure index
            UseIndexAndTaxonomyWriter(index, (writer, tw) =>
            {
                writer.Commit();
                tw.Commit();
                return true;
            }, storage);
        }

        using LuceneDirectory indexDir = FSDirectory.Open(storage.Path);
        using var reader = DirectoryReader.Open(indexDir);
        var searcher = new IndexSearcher(reader);

        using var taxonomyDir = FSDirectory.Open(storage.TaxonomyPath);

        using var taxonomyReader = new DirectoryTaxonomyReader(taxonomyDir);
        var facetsCollector = new FacetsCollector();
        Dictionary<string, Facets> facetsMap = new Dictionary<string, Facets>();
        FacetsCollector.Search(searcher, query, n, facetsCollector);
        var config = index.LuceneIndexingStrategy.FacetsConfigFactory();
        OrdinalsReader ordinalsReader = new DocValuesOrdinalsReader(FacetsConfig.DEFAULT_INDEX_FIELD_NAME);
        var facetCounts = new TaxonomyFacetCounts(ordinalsReader, taxonomyReader, config, facetsCollector);
        var facets = new MultiFacets(facetsMap, facetCounts);

        var results = useIndexSearcher(searcher, facets);

        return results;

    }
}
