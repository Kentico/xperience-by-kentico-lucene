using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kentico.Xperience.Lucene.Models;
using CMS.IO;
using Lucene.Net.Store;
using LuceneDirectory = Lucene.Net.Store.Directory;
using Lucene.Net.Search;
using Microsoft.Extensions.Logging;

namespace Kentico.Xperience.Lucene.Services.Implementations
{
    public class DefaultLuceneIndexService : ILuceneIndexService
    {
        const LuceneVersion luceneVersion = LuceneVersion.LUCENE_48;

        public TResult UseWriter<TResult>(LuceneIndex index, Func<IndexWriter, TResult> useIndexWriter)
        {
            using LuceneDirectory indexDir = FSDirectory.Open(index.IndexPath);

            //Create an index writer
            IndexWriterConfig indexConfig = new IndexWriterConfig(luceneVersion, index.Analyzer);
            indexConfig.OpenMode = OpenMode.CREATE_OR_APPEND;                             // create/overwrite index
            using IndexWriter writer = new IndexWriter(indexDir, indexConfig);

            return useIndexWriter(writer);
        }

        public TResult UseSearcher<TResult>(LuceneIndex index, Func<IndexSearcher, TResult> useIndexSearcher)
        {
            if (!System.IO.Directory.Exists(index.IndexPath))
            {
                // ensure index
                UseWriter(index, (writer) => {
                    writer.Commit();
                    return true; });
            }
            using LuceneDirectory indexDir = FSDirectory.Open(index.IndexPath);
            using var reader = DirectoryReader.Open(indexDir);
            IndexSearcher searcher = new IndexSearcher(reader);
            return useIndexSearcher(searcher);
        }
    }
}
