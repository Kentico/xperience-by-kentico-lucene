using Microsoft.Extensions.Hosting;

namespace Kentico.Xperience.Lucene.Core.Indexing;

internal static class LuceneIndexLockHelper
{
    private const string LOCK_FILE_NAME = "index.lock";


    /// <summary>
    /// Lock wait timeout duration for acquiring the lock on the local file system.
    /// This should be long enough to allow for index operations to complete, but not so long that it causes excessive delays in case of issues.
    /// </summary>
    internal static readonly TimeSpan LOCK_WAIT_TIMEOUT = TimeSpan.FromSeconds(30);


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
