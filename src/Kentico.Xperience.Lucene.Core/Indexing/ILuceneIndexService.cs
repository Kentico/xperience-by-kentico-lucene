using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Index;

namespace Kentico.Xperience.Lucene.Core.Indexing;

public interface ILuceneIndexService
{
    public T UseIndexAndTaxonomyWriter<T>(LuceneIndex index, Func<IndexWriter, ITaxonomyWriter, T> useIndexWriter, IndexStorageModel storage, OpenMode openMode = OpenMode.CREATE_OR_APPEND);

    public T UseWriter<T>(LuceneIndex index, Func<IndexWriter, T> useIndexWriter, IndexStorageModel storage, OpenMode openMode = OpenMode.CREATE_OR_APPEND);

    public void ResetIndex(LuceneIndex index);
}
