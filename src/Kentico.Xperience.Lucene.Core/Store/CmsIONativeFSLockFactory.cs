using Lucene.Net.Store;
using Lucene.Net.Util;

using CmsDirectory = CMS.IO.Directory;
using CmsFile = CMS.IO.File;
using CmsPath = CMS.IO.Path;
using CmsFileStream = CMS.IO.FileStream;
using CmsFileMode = CMS.IO.FileMode;
using CmsFileAccess = CMS.IO.FileAccess;
using CmsFileShare = CMS.IO.FileShare;

namespace Kentico.Xperience.Lucene.Core.Store;

/// <summary>
/// Implements <see cref="LockFactory"/> using CMS.IO file operations.
/// </summary>
/// <remarks>
/// <para>
/// This is a CMS.IO-based equivalent of NativeFSLockFactory. Since CMS.IO abstracts
/// different storage providers, true native file locking may not be available on all
/// storage backends (e.g., Azure Blob Storage).
/// </para>
/// <para>
/// This implementation uses file existence and exclusive write access to simulate locking.
/// For local filesystem storage, this provides similar semantics to NativeFSLockFactory.
/// For cloud storage, the behavior depends on the provider's support for exclusive access.
/// </para>
/// </remarks>
public class CmsIONativeFSLockFactory : CmsIOFSLockFactory
{
    /// <summary>
    /// Singleton dictionary tracking lock instances
    /// </summary>
    internal static readonly Dictionary<string, Lock> _locks = [];

    /// <summary>
    /// Create a CmsIONativeFSLockFactory instance, with null (unset) lock directory
    /// </summary>
    public CmsIONativeFSLockFactory()
        : this(null)
    {
    }

    /// <summary>
    /// Create a CmsIONativeFSLockFactory instance, storing lock files into the specified lockDirName
    /// </summary>
    /// <param name="lockDirName">where lock files are created</param>
    public CmsIONativeFSLockFactory(string? lockDirName)
    {
        if (lockDirName != null)
        {
            SetLockDir(lockDirName);
        }
    }

    /// <summary>
    /// Given a lock name, return the full prefixed path of the actual lock file
    /// </summary>
    private string GetCanonicalPathOfLockFile(string lockName)
    {
        if (m_lockPrefix != null)
        {
            lockName = m_lockPrefix + "-" + lockName;
        }
        return CmsPath.Combine(m_lockDir!, lockName).Replace('\\', '/');
    }

    public override Lock MakeLock(string lockName)
    {
        string path = GetCanonicalPathOfLockFile(lockName);
        Lock l;
        lock (_locks)
        {
            if (!_locks.TryGetValue(path, out l!))
            {
                _locks.Add(path, l = NewLock(path));
            }
        }
        return l;
    }

    // Internal for testing
    internal virtual Lock NewLock(string path)
    {
        return new CmsIONativeFSLock(m_lockDir!, path);
    }

    public override void ClearLock(string lockName)
    {
        string path = GetCanonicalPathOfLockFile(lockName);
        lock (_locks)
        {
            if (_locks.TryGetValue(path, out Lock? l))
            {
                _locks.Remove(path);
                l?.Dispose();
            }
        }
    }
}

/// <summary>
/// Native file system lock implementation using CMS.IO
/// </summary>
internal class CmsIONativeFSLock : Lock
{
    private CmsFileStream? channel;
    private readonly string path;
    private readonly string lockDir;

    public CmsIONativeFSLock(string lockDir, string path)
    {
        this.lockDir = lockDir;
        this.path = path;
    }

    public override bool Obtain()
    {
        lock (this)
        {
            FailureReason = null;

            if (channel != null)
            {
                // Our instance is already locked
                return false;
            }

            string? directory = CmsPath.GetDirectoryName(path);

            if (!string.IsNullOrEmpty(directory) && !CmsDirectory.Exists(directory))
            {
                try
                {
                    CmsDirectory.CreateDirectory(directory);
                }
                catch (Exception e)
                {
                    // Note that several processes might have been trying to create the same directory at the same time.
                    // If one succeeded, the directory will exist and the exception can be ignored. In all other cases we should report it.
                    if (!CmsDirectory.Exists(directory))
                    {
                        throw new IOException($"Cannot create directory: {lockDir}", e);
                    }
                }
            }
            else if (CmsFile.Exists(directory))
            {
                throw new IOException($"Found regular file where directory expected: {lockDir}");
            }

            bool success = false;
            try
            {
                // Try to create the file with exclusive access
                // CMS.IO.FileShare.None attempts to get exclusive access
                channel = CmsFileStream.New(path, CmsFileMode.OpenOrCreate, CmsFileAccess.Write, CmsFileShare.None);
                success = true;
            }
            catch (Exception e)
            {
                FailureReason = e;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.DisposeWhileHandlingException(channel);
                    channel = null;
                }
            }

            return channel != null;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            lock (this)
            {
                // Whether or not we have created a file, we need to remove
                // the lock instance from the dictionary that tracks them.
                try
                {
                    lock (CmsIONativeFSLockFactory._locks)
                    {
                        CmsIONativeFSLockFactory._locks.Remove(path);
                    }
                }
                finally
                {
                    if (channel != null)
                    {
                        try
                        {
                            IOUtils.DisposeWhileHandlingException(channel);
                        }
                        finally
                        {
                            channel = null;
                        }

                        // Try to delete the file if we created it, but it's not an error if we can't
                        try
                        {
                            if (CmsFile.Exists(path))
                            {
                                CmsFile.Delete(path);
                            }
                        }
                        catch
                        {
                            // Ignore deletion failures
                        }
                    }
                }
            }
        }
    }

    public override bool IsLocked()
    {
        lock (this)
        {
            // First a shortcut, if a lock reference in this instance is available
            if (channel != null)
            {
                return true;
            }

            // Look if lock file is present; if not, there can definitely be no lock!
            if (!CmsFile.Exists(path))
            {
                return false;
            }

            // Try to obtain and release (if was locked) the lock
            try
            {
                bool obtained = Obtain();
                if (obtained)
                {
                    Dispose();
                }
                return !obtained;
            }
            catch (Exception ioe)
            {
                return false;
            }
        }
    }

    public override string ToString()
        => $"{nameof(CmsIONativeFSLock)}@{path}";
}
