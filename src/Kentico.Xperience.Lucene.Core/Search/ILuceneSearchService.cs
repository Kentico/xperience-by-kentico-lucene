using Kentico.Xperience.Lucene.Core.Indexing;

using Lucene.Net.Facet;
using Lucene.Net.Search;

namespace Kentico.Xperience.Lucene.Core.Search;

/// <summary>
/// Primary service used for querying lucene indexes
/// </summary>
public interface ILuceneSearchService
{
    public TResult UseSearcher<TResult>(LuceneIndex index, Func<IndexSearcher, TResult> useIndexSearcher);

    public TResult UseSearcherWithFacets<TResult>(LuceneIndex index, Query query, int n, Func<IndexSearcher, MultiFacets, TResult> useIndexSearcher);
}
