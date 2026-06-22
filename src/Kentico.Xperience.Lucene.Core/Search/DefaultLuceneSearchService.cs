using Kentico.Xperience.Lucene.Core.Indexing;

using Lucene.Net.Facet;
using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Index;
using Lucene.Net.Search;

using Microsoft.Extensions.DependencyInjection;

namespace Kentico.Xperience.Lucene.Core.Search;

internal class DefaultLuceneSearchService : ILuceneSearchService
{
    private readonly LuceneIndexSearcherProvider searcherProvider;
    private readonly IServiceProvider serviceProvider;

    public DefaultLuceneSearchService(LuceneIndexSearcherProvider searcherProvider, IServiceProvider serviceProvider)
    {
        this.searcherProvider = searcherProvider;
        this.serviceProvider = serviceProvider;
    }


    /// <inheritdoc />
    public TResult UseSearcher<TResult>(LuceneIndex index, Func<IndexSearcher, TResult> useIndexSearcher)
    {
        using var lease = searcherProvider.Acquire(index);
        return useIndexSearcher(lease.Searcher);
    }


    /// <inheritdoc />
    public TResult UseSearcherWithFacets<TResult>(LuceneIndex index, Query query, int n, Func<IndexSearcher, MultiFacets, TResult> useIndexSearcher)
    {
        using var lease = searcherProvider.Acquire(index, withTaxonomy: true);
        var searcher = lease.Searcher;

        var facetsCollector = new FacetsCollector();
        Dictionary<string, Facets> facetsMap = [];
        FacetsCollector.Search(searcher, query, n, facetsCollector);
        var strategy = serviceProvider.GetRequiredStrategy(index);
        var config = strategy?.FacetsConfigFactory() ?? new FacetsConfig();
        OrdinalsReader ordinalsReader = new DocValuesOrdinalsReader(FacetsConfig.DEFAULT_INDEX_FIELD_NAME);
        var facetCounts = new TaxonomyFacetCounts(ordinalsReader, lease.TaxonomyReader, config, facetsCollector);
        var facets = new MultiFacets(facetsMap, facetCounts);

        var results = useIndexSearcher(searcher, facets);

        return results;
    }


    /// <inheritdoc />
    public TResult UseSearcherWithDrillSideways<TResult>(LuceneIndex index, Func<IndexSearcher, DrillSideways, TResult> useIndexSearcher)
    {
        using var lease = searcherProvider.Acquire(index, withTaxonomy: true);
        var searcher = lease.Searcher;

        var strategy = serviceProvider.GetRequiredStrategy(index);
        var config = strategy?.FacetsConfigFactory() ?? new FacetsConfig();

        var drillSideways = new DrillSideways(searcher, config, lease.TaxonomyReader);

        var results = useIndexSearcher(searcher, drillSideways);

        return results;
    }
}
