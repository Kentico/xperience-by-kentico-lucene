using System.Reflection;
using Kentico.Xperience.Lucene.Attributes;
using Kentico.Xperience.Lucene.Services;
using Kentico.Xperience.Lucene.Services.Implementations;
using Lucene.Net.Analysis;

namespace Kentico.Xperience.Lucene.Models
{
    /// <summary>
    /// Represents the configuration of an Lucene index.
    /// </summary>
    public sealed class LuceneIndex
    {
        /// <summary>
        /// Lucene Analyzer instance <see cref="Analyzer"/>.
        /// </summary>
        public Analyzer Analyzer { get; }

        /// <summary>
        /// The type of the class which extends <see cref="LuceneSearchModel"/>.
        /// </summary>
        public Type LuceneSearchModelType
        {
            get;
        }

        /// <summary>
        /// The type of the class which extends <see cref="ILuceneIndexingStrategy"/>.
        /// </summary>
        public ILuceneIndexingStrategy LuceneIndexingStrategy
        {
            get;
        }


        /// <summary>
        /// The code name of the Lucene index.
        /// </summary>
        public string IndexName
        {
            get;
        }

        /// <summary>
        /// The filesystem path of the Lucene index.
        /// </summary>
        public string IndexPath
        {
            get;
        }


        /// <summary>
        /// An arbitrary ID used to identify the Lucene index in the admin UI.
        /// </summary>
        internal int Identifier
        {
            get;
            set;
        }


        /// <summary>
        /// The <see cref="IncludedPathAttribute"/>s which are defined in the search model.
        /// </summary>
        internal IEnumerable<IncludedPathAttribute> IncludedPaths
        {
            get;
            set;
        }


        /// <summary>
        /// Initializes a new <see cref="LuceneIndex"/>.
        /// </summary>
        /// <param name="type">The type of the class which extends <see cref="LuceneSearchModel"/>.</param>
        /// <param name="analyzer">Lucene Analyzer instance <see cref="Analyzer"/>.</param>
        /// <param name="indexName">The code name of the Lucene index.</param>
        /// <param name="indexPath">The filesystem Lucene index. Defaults to /App_Data/LuceneSearch/[IndexName]</param>
        /// <param name="luceneIndexingStrategy">Defaults to  <see cref="DefaultLuceneIndexingStrategy"/></param>
        /// <exception cref="ArgumentNullException" />
        /// <exception cref="InvalidOperationException" />
        public LuceneIndex(Type type, Analyzer analyzer, string indexName, string? indexPath = null, ILuceneIndexingStrategy? luceneIndexingStrategy = null)
        {
            if (string.IsNullOrEmpty(indexName))
            {
                throw new ArgumentNullException(nameof(indexName));
            }

            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (!typeof(LuceneSearchModel).IsAssignableFrom(type))
            {
                throw new InvalidOperationException($"The search model {type} must extend {nameof(LuceneSearchModel)}.");
            }

            Analyzer = analyzer ?? throw new ArgumentNullException(nameof(analyzer));
            LuceneSearchModelType = type;
            IndexName = indexName;
            IndexPath = indexPath ?? Path.Combine(Environment.CurrentDirectory, "App_Data", "LuceneSearch", indexName);
            LuceneIndexingStrategy = luceneIndexingStrategy ?? new DefaultLuceneIndexingStrategy();

            var paths = type.GetCustomAttributes<IncludedPathAttribute>(false);
            foreach (var path in paths)
            {
                path.Identifier = Guid.NewGuid().ToString();
            }

            IncludedPaths = paths;
        }
    }
}
