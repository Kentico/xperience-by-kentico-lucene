using Kentico.Xperience.Lucene.Core.Indexing;

using Lucene.Net.Facet;
using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Facet.Taxonomy.Directory;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;

using Microsoft.Extensions.DependencyInjection;

using LuceneDirectory = Lucene.Net.Store.Directory;

namespace Kentico.Xperience.Lucene.Core.Search;

internal class DefaultLuceneSearchService : ILuceneSearchService
{
    private readonly ILuceneIndexService indexService;
    private readonly IServiceProvider serviceProvider;

    public DefaultLuceneSearchService(ILuceneIndexService indexService, IServiceProvider serviceProvider)
    {
        this.indexService = indexService;
        this.serviceProvider = serviceProvider;
    }

    public TResult UseSearcher<TResult>(LuceneIndex index, Func<IndexSearcher, TResult> useIndexSearcher)
    {
        var storage = index.StorageContext.GetPublishedIndex();
        if (!System.IO.Directory.Exists(storage.Path))
        {
            // ensure index
            indexService.UseWriter(index, (writer) =>
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
            indexService.UseIndexAndTaxonomyWriter(index, (writer, tw) =>
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
        Dictionary<string, Facets> facetsMap = [];
        FacetsCollector.Search(searcher, query, n, facetsCollector);
        var strategy = serviceProvider.GetRequiredStrategy(index);
        var config = strategy?.FacetsConfigFactory() ?? new FacetsConfig();
        OrdinalsReader ordinalsReader = new DocValuesOrdinalsReader(FacetsConfig.DEFAULT_INDEX_FIELD_NAME);
        var facetCounts = new TaxonomyFacetCounts(ordinalsReader, taxonomyReader, config, facetsCollector);
        var facets = new MultiFacets(facetsMap, facetCounts);

        var results = useIndexSearcher(searcher, facets);

        return results;

    }
}
