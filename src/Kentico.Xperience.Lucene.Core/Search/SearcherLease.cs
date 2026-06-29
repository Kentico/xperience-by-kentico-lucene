using Lucene.Net.Facet.Taxonomy.Directory;
using Lucene.Net.Search;

namespace Kentico.Xperience.Lucene.Core.Search;

/// <summary>
/// A reference to a cached <see cref="IndexSearcher"/> (and optional taxonomy reader). Dispose to return
/// the references to the owning <see cref="LuceneIndexSearcherProvider"/>.
/// </summary>
internal sealed class SearcherLease : IDisposable
{
    private readonly Action releaseCallback;
    private bool disposed;


    internal SearcherLease(IndexSearcher searcher, DirectoryTaxonomyReader? taxonomyReader, Action releaseCallback)
    {
        Searcher = searcher;
        TaxonomyReader = taxonomyReader;
        this.releaseCallback = releaseCallback;
    }


    /// <summary>
    /// The searcher over the current published index generation. Valid until the lease is disposed.
    /// </summary>
    public IndexSearcher Searcher { get; }


    /// <summary>
    /// The taxonomy reader for faceted search, or <see langword="null"/> when the lease was acquired
    /// without taxonomy support.
    /// </summary>
    public DirectoryTaxonomyReader? TaxonomyReader { get; }


    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        releaseCallback();
    }
}
