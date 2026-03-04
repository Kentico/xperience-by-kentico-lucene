using Microsoft.Extensions.Hosting;

namespace Kentico.Xperience.Lucene.Core.Indexing;

internal static class LuceneIndexLockHelper
{
    internal const string LOCK_FILE_NAME = "index.lock";

    /// <summary>
    /// Gets the path to the shared lock file on the local file system for the given index.
    /// The same lock file is used by both readers and writers to ensure mutual exclusion.
    /// </summary>
    internal static string GetLockFilePath(string indexName, IHostEnvironment environment)
    {
        // Lock file is stored on local file system, not in CMS.IO storage
        var lockDir = Path.Combine(environment.ContentRootPath, "$LuceneLocks", indexName);
        return Path.Combine(lockDir, LOCK_FILE_NAME);
    }
}
