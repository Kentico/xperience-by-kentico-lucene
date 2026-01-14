using Kentico.Xperience.Lucene.Core.Indexing;

using Lucene.Net.Facet;
using Lucene.Net.Search;

namespace Kentico.Xperience.Lucene.Core.Search;

/// <summary>
/// Primary service used for querying lucene indexes
/// </summary>
public interface ILuceneSearchService
{
    /// <summary>
    /// Executes a search operation using a Lucene <see cref="IndexSearcher"/>.
    /// </summary>
    /// <typeparam name="TResult">The type of the result returned by the search operation.</typeparam>
    /// <param name="index">The Lucene index to search.</param>
    /// <param name="useIndexSearcher">A function that performs the search using the <see cref="IndexSearcher"/>.</param>
    /// <returns>The result of the search operation.</returns>
    TResult UseSearcher<TResult>(LuceneIndex index, Func<IndexSearcher, TResult> useIndexSearcher);


    /// <summary>
    /// Executes a search operation with faceted search capabilities using <see cref="MultiFacets"/>.
    /// </summary>
    /// <typeparam name="TResult">The type of the result returned by the search operation.</typeparam>
    /// <param name="index">The Lucene index to search.</param>
    /// <param name="query">The query to execute for collecting facet data.</param>
    /// <param name="n">The maximum number of results to collect for faceting.</param>
    /// <param name="useIndexSearcher">A function that performs the search using the <see cref="IndexSearcher"/> and <see cref="MultiFacets"/>.</param>
    /// <returns>The result of the search operation with facet information.</returns>
    TResult UseSearcherWithFacets<TResult>(LuceneIndex index, Query query, int n, Func<IndexSearcher, MultiFacets, TResult> useIndexSearcher);


    /// <summary>
    /// Executes a search operation with drill-sideways faceted search capabilities using <see cref="DrillSideways"/>.
    /// This method enables drill-down queries while maintaining facet counts for unchosen facets,
    /// allowing users to see what results would be available if they chose different facet values.
    /// </summary>
    /// <typeparam name="TResult">The type of the result returned by the search operation.</typeparam>
    /// <param name="index">The Lucene index to search.</param>
    /// <param name="useIndexSearcher">A function that performs the search using the <see cref="IndexSearcher"/> and <see cref="DrillSideways"/>.</param>
    /// <returns>The result of the search operation with drill-sideways facet information.</returns>
    TResult UseSearcherWithDrillSideways<TResult>(LuceneIndex index, Func<IndexSearcher, DrillSideways, TResult> useIndexSearcher);
}
