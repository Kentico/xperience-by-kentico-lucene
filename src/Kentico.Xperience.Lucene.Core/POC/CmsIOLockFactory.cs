using CMS.Helpers.Synchronization;

using Kentico.Xperience.Lucene.Core.Indexing;

using Lucene.Net.Store;

using Microsoft.Extensions.Hosting;

namespace Kentico.Xperience.Lucene.Core.POC;

/// <summary>
/// A Lucene LockFactory implementation that stores the lock file on the local file system
/// using <see cref="LuceneIndexLockHelper"/> to resolve the lock path, ensuring locking
/// works correctly even when the index is stored in CMS.IO-backed (e.g. Azure Blob) storage.
/// </summary>
public class CmsIOLockFactory : LockFactory
{
    private readonly string indexName;
    private readonly IHostEnvironment environment;

    /// <summary>
    /// Creates a new CmsIOLockFactory for the specified index.
    /// </summary>
    /// <param name="indexName">The name of the Lucene index.</param>
    /// <param name="environment">The host environment used to resolve the local lock directory.</param>
    public CmsIOLockFactory(string indexName, IHostEnvironment environment)
    {
        this.indexName = indexName ?? throw new ArgumentNullException(nameof(indexName));
        this.environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    /// <inheritdoc/>
    public override Lock MakeLock(string lockName)
    {
        string lockPath = LuceneIndexLockHelper.GetLockFilePath(indexName, environment);
        return new CmsIOLock(lockPath);
    }

    /// <inheritdoc/>
    public override void ClearLock(string lockName)
    {
        string lockPath = LuceneIndexLockHelper.GetLockFilePath(indexName, environment);
        if (File.Exists(lockPath))
        {
            try
            {
                File.Delete(lockPath);
            }
            catch (IOException)
            {
                // Lock file may be held by another process - this is expected in some scenarios
            }
        }
    }
}

/// <summary>
/// A Lucene Lock implementation backed by <see cref="FileLock"/>.
/// </summary>
public class CmsIOLock : Lock
{
    private readonly string lockPath;
    private FileLock? fileLock;

    /// <summary>
    /// Creates a new CmsIOLock for the specified lock file path.
    /// </summary>
    /// <param name="lockPath">The full path to the lock file.</param>
    public CmsIOLock(string lockPath) => this.lockPath = lockPath ?? throw new ArgumentNullException(nameof(lockPath));

    /// <inheritdoc/>
    public override bool Obtain()
    {
        if (fileLock != null)
        {
            return true;
        }

        var fl = new FileLock(lockPath);
        if (fl.TryGetLock())
        {
            fileLock = fl;
            return true;
        }

        //fl.Dispose();
        return false;
    }

    /// <inheritdoc/>
    public override bool IsLocked() => fileLock != null;

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            fileLock?.Dispose();
            fileLock = null;
        }
    }
}
