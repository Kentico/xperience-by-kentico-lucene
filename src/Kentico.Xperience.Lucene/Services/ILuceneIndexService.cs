using Kentico.Xperience.Lucene.Models;
using Lucene.Net.Index;
using Lucene.Net.Search;
using System;

namespace Kentico.Xperience.Lucene.Services
{
    public interface ILuceneIndexService
    {
        T UseWriter<T>(LuceneIndex index, Func<IndexWriter, T> useIndexWriter);

        TResult UseSearcher<TResult>(LuceneIndex index, Func<IndexSearcher, TResult> useIndexSearcher);
    }
}