using Kentico.Xperience.Lucene.Models;
using Lucene.Net.Index;
using Lucene.Net.Search;

namespace Kentico.Xperience.Lucene.Services;

public interface ILuceneIndexService
{
    T UseWriter<T>(LuceneIndex index, Func<IndexWriter, T> useIndexWriter, OpenMode openMode = OpenMode.CREATE_OR_APPEND);

    void ResetIndex(LuceneIndex index);

    TResult UseSearcher<TResult>(LuceneIndex index, Func<IndexSearcher, TResult> useIndexSearcher);
}
