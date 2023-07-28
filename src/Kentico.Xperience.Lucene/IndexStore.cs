using Kentico.Xperience.Lucene.Models;

namespace Kentico.Xperience.Lucene
{
    /// <summary>
    /// Represents a store of Lucene indexes and crawlers.
    /// </summary>
    public sealed class IndexStore
    {
        private static readonly Lazy<IndexStore> mInstance = new();
        private readonly List<LuceneIndex> registeredIndexes = new();
        private readonly HashSet<string> registeredCrawlers = new();


        /// <summary>
        /// Gets current instance of the <see cref="IndexStore"/> class.
        /// </summary>
        public static IndexStore Instance => mInstance.Value;


        /// <summary>
        /// Adds an index to the store.
        /// </summary>
        /// <param name="index">The index to add.</param>
        /// <exception cref="ArgumentNullException" />
        /// <exception cref="InvalidOperationException" />
        public void AddIndex(LuceneIndex index)
        {
            if (index == null)
            {
                throw new ArgumentNullException(nameof(index));
            }

            if (registeredIndexes.Any(i => i.IndexName.Equals(index.IndexName, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"Attempted to register Lucene index with name '{index.IndexName},' but it is already registered.");
            }

            //AddIncludedPaths(index);

            index.Identifier = registeredIndexes.Count + 1;
            registeredIndexes.Add(index);
        }


        /// <summary>
        /// Adds a crawler to the store.
        /// </summary>
        /// <param name="crawlerId">The ID of the crawler to add.</param>
        /// <exception cref="ArgumentNullException" />
        /// <exception cref="InvalidOperationException" />
        public void AddCrawler(string crawlerId)
        {
            if (string.IsNullOrEmpty(crawlerId))
            {
                throw new ArgumentNullException(crawlerId);
            }

            if (registeredCrawlers.Any(id => id.Equals(crawlerId, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"Attempted to register Lucene crawler with ID '{crawlerId},' but it is already registered.");
            }

            registeredCrawlers.Add(crawlerId);
        }


        /// <summary>
        /// Gets a registered <see cref="LuceneIndex"/> with the specified <paramref name="indexName"/>,
        /// or <c>null</c>.
        /// </summary>
        /// <param name="indexName">The name of the index to retrieve.</param>
        /// <exception cref="ArgumentNullException" />
        /// <exception cref="InvalidOperationException" />
        public LuceneIndex? GetIndex(string indexName)
        {
            if (string.IsNullOrEmpty(indexName))
            {
                throw new ArgumentNullException(nameof(indexName));
            }

            return registeredIndexes.SingleOrDefault(i => i.IndexName.Equals(indexName, StringComparison.OrdinalIgnoreCase));
        }


        /// <summary>
        /// Gets all registered indexes.
        /// </summary>
        public IEnumerable<LuceneIndex> GetAllIndexes() => registeredIndexes;


        /// <summary>
        /// Gets all registered crawlers.
        /// </summary>
        public IEnumerable<string> GetAllCrawlers() => registeredCrawlers;


        //private static void AddIncludedPaths(LuceneIndex index)
        //{
        //    var paths = index.LuceneSearchModelType.GetCustomAttributes<IncludedPathAttribute>(false);
        //    foreach (var path in paths)
        //    {
        //        path.Identifier = Guid.NewGuid().ToString();
        //    }

        //    index.IncludedPaths = paths;
        //}


        internal void ClearCrawlers() => registeredCrawlers.Clear();


        internal void ClearIndexes() => registeredIndexes.Clear();


        internal LuceneIndex? GetIndex(int id) => registeredIndexes.FirstOrDefault(i => i.Identifier == id);
    }
}
