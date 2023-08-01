using Kentico.Xperience.Lucene.Models;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using LuceneDirectory = Lucene.Net.Store.Directory;

namespace Kentico.Xperience.Lucene.Services.Implementations;

public class DefaultLuceneIndexService : ILuceneIndexService
{
    private const LuceneVersion luceneVersion = LuceneVersion.LUCENE_48;

    public TResult UseWriter<TResult>(LuceneIndex index, Func<IndexWriter, TResult> useIndexWriter)
    {
        using LuceneDirectory indexDir = FSDirectory.Open(index.IndexPath);

        //Create an index writer
        var indexConfig = new IndexWriterConfig(luceneVersion, index.Analyzer)
        {
            OpenMode = OpenMode.CREATE_OR_APPEND                             // create/overwrite index
        };
        using var writer = new IndexWriter(indexDir, indexConfig);

        return useIndexWriter(writer);
    }

    public TResult UseSearcher<TResult>(LuceneIndex index, Func<IndexSearcher, TResult> useIndexSearcher)
    {
        if (!System.IO.Directory.Exists(index.IndexPath))
        {
            // ensure index
            UseWriter(index, (writer) =>
            {
                writer.Commit();
                return true;
            });
        }
        using LuceneDirectory indexDir = FSDirectory.Open(index.IndexPath);
        using var reader = DirectoryReader.Open(indexDir);
        var searcher = new IndexSearcher(reader);
        return useIndexSearcher(searcher);
    }
}
