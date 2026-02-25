using Lucene.Net.Store;

using CmsDirectory = CMS.IO.Directory;
using CmsFile = CMS.IO.File;
using CmsFileStream = CMS.IO.FileStream;
using CmsPath = CMS.IO.Path;
using CmsFileMode = CMS.IO.FileMode;
using CmsFileAccess = CMS.IO.FileAccess;
using CmsFileShare = CMS.IO.FileShare;

namespace Kentico.Xperience.Lucene.Core.Store;

/// <summary>
/// A Lucene LockFactory implementation that uses CMS.IO for file-based locking.
/// This enables Lucene's index locking mechanism to work with Azure Blob Storage
/// or other custom storage providers through the CMS.IO abstraction.
/// </summary>
public class CmsIOLockFactory : LockFactory
{
    private readonly string lockDir;

    /// <summary>
    /// Creates a new CmsIOLockFactory for the specified directory.
    /// </summary>
    /// <param name="lockDir">The directory where lock files will be created.</param>
    public CmsIOLockFactory(string lockDir) => this.lockDir = lockDir ?? throw new ArgumentNullException(nameof(lockDir));

    /// <summary>
    /// Creates a Lock instance for the given lock name.
    /// </summary>
    /// <param name="lockName">The name of the lock (typically "write.lock").</param>
    /// <returns>A Lock instance that can be used to obtain/release the lock.</returns>
    public override Lock MakeLock(string lockName)
    {
        string lockPath = CmsPath.Combine(lockDir, lockName);
        return new CmsIOLock(lockPath);
    }

    /// <summary>
    /// Clears (forcefully removes) an existing lock.
    /// </summary>
    /// <param name="lockName">The name of the lock to clear.</param>
    public override void ClearLock(string lockName)
    {
        string lockPath = CmsPath.Combine(lockDir, lockName);
        if (CmsFile.Exists(lockPath))
        {
            try
            {
                CmsFile.Delete(lockPath);
            }
            catch (IOException)
            {
                // Lock file may be held by another process - this is expected in some scenarios
            }
        }
    }
}

/// <summary>
/// A Lucene Lock implementation using CMS.IO file operations.
/// Uses a simple file-existence-based locking mechanism.
/// </summary>
public class CmsIOLock : Lock
{
    private readonly string lockPath;
    private CmsFileStream? lockStream;

    /// <summary>
    /// Creates a new CmsIOLock for the specified lock file path.
    /// </summary>
    /// <param name="lockPath">The full path to the lock file.</param>
    public CmsIOLock(string lockPath) => this.lockPath = lockPath ?? throw new ArgumentNullException(nameof(lockPath));

    /// <summary>
    /// Attempts to obtain the lock.
    /// </summary>
    /// <returns>True if the lock was obtained, false otherwise.</returns>
    public override bool Obtain()
    {
        if (lockStream != null)
        {
            // Already holding the lock
            return true;
        }

        try
        {
            // Ensure the directory exists
            string? directory = CmsPath.GetDirectoryName(lockPath);
            if (!string.IsNullOrEmpty(directory) && !CmsDirectory.Exists(directory))
            {
                CmsDirectory.CreateDirectory(directory);
            }

            // Try to create the lock file with exclusive access
            // If the file already exists and is locked, this will throw
            lockStream = CmsFileStream.New(
                lockPath,
                CmsFileMode.Create,
                CmsFileAccess.Write,
                CmsFileShare.None);

            // Write some content to the lock file for debugging purposes
            var bytes = System.Text.Encoding.UTF8.GetBytes(
                $"Lock obtained at {DateTime.UtcNow:O}\n" +
                $"Process: {Environment.ProcessId}\n" +
                $"Machine: {Environment.MachineName}\n");
            lockStream.Write(bytes, 0, bytes.Length);
            lockStream.Flush();

            return true;
        }
        catch (IOException)
        {
            // Could not obtain lock - file is likely held by another process
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            // Permission denied
            return false;
        }
    }

    /// <summary>
    /// Gets whether this lock is currently held.
    /// </summary>
    public override bool IsLocked()
    {
        if (lockStream != null)
        {
            return true;
        }

        // Check if the lock file exists and is locked by another process
        if (!CmsFile.Exists(lockPath))
        {
            return false;
        }

        // Try to open the file to see if it's locked
        try
        {
            using var testStream = CmsFileStream.New(
                lockPath,
                CmsFileMode.Open,
                CmsFileAccess.Write,
                CmsFileShare.None);
            // If we get here, the file wasn't locked
            return false;
        }
        catch (IOException)
        {
            // File is locked
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            // Can't access - assume locked
            return true;
        }
    }

    /// <summary>
    /// Releases the lock.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (!disposing || lockStream == null)
        {
            return;
        }

        try
        {
            lockStream.Dispose();
        }
        finally
        {
            lockStream = null;
        }

        // Try to delete the lock file
        try
        {
            if (CmsFile.Exists(lockPath))
            {
                CmsFile.Delete(lockPath);
            }
        }
        catch (IOException)
        {
            // Ignore - file may be immediately grabbed by another process
        }
    }
}
