using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Index;

namespace Kentico.Xperience.Lucene.Core.Indexing;

public interface ILuceneIndexService
{
    T UseIndexAndTaxonomyWriter<T>(LuceneIndex index, Func<IndexWriter, ITaxonomyWriter, T> useIndexWriter, IndexStorageModel storage, OpenMode openMode = OpenMode.CREATE_OR_APPEND);

    T UseWriter<T>(LuceneIndex index, Func<IndexWriter, T> useIndexWriter, IndexStorageModel storage, OpenMode openMode = OpenMode.CREATE_OR_APPEND);

    void ResetIndex(LuceneIndex index);
}
