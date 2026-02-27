using Lucene.Net.Store;

using System.Text;

using CmsDirectory = CMS.IO.Directory;
using CmsFile = CMS.IO.File;
using CmsPath = CMS.IO.Path;

namespace Kentico.Xperience.Lucene.Core.Store;

/// <summary>
/// Implements <see cref="LockFactory"/> using CMS.IO file creation.
/// </summary>
/// <remarks>
/// <para>
/// This is a CMS.IO-based equivalent of SimpleFSLockFactory. It creates lock files
/// to indicate locks, which works reliably across all storage providers supported by CMS.IO.
/// </para>
/// <para>
/// Special care needs to be taken if you change the locking implementation: First be certain
/// that no writer is in fact writing to the index otherwise you can easily corrupt your index.
/// Be sure to do the <see cref="LockFactory"/> change to all Lucene instances and clean up
/// all leftover lock files before starting the new configuration for the first time.
/// Different implementations can not work together!
/// </para>
/// </remarks>
public class CmsIOSimpleFSLockFactory : CmsIOFSLockFactory
{
    /// <summary>
    /// Create a CmsIOSimpleFSLockFactory instance, with null (unset) lock directory
    /// </summary>
    public CmsIOSimpleFSLockFactory()
        : this(null)
    {
    }

    /// <summary>
    /// Instantiate using the provided directory name
    /// </summary>
    /// <param name="lockDirName">where lock files should be created</param>
    public CmsIOSimpleFSLockFactory(string? lockDirName)
    {
        if (lockDirName != null)
        {
            SetLockDir(lockDirName);
        }
    }

    public override Lock MakeLock(string lockName)
    {
        if (m_lockPrefix != null)
        {
            lockName = m_lockPrefix + "-" + lockName;
        }
        return new CmsIOSimpleFSLock(m_lockDir!, lockName);
    }

    public override void ClearLock(string lockName)
    {
        if (m_lockDir != null && CmsDirectory.Exists(m_lockDir))
        {
            if (m_lockPrefix != null)
            {
                lockName = m_lockPrefix + "-" + lockName;
            }

            string lockFile = CmsPath.Combine(m_lockDir, lockName);
            try
            {
                if (CmsFile.Exists(lockFile))
                {
                    CmsFile.Delete(lockFile);
                }
            }
            catch (Exception e)
            {
                if (CmsFile.Exists(lockFile)) // Delete failed and lockFile exists
                {
                    throw new IOException($"Cannot delete {lockFile}", e);
                }
            }
        }
    }
}

/// <summary>
/// Simple file system lock implementation using CMS.IO
/// </summary>
internal class CmsIOSimpleFSLock : Lock
{
    internal string LockFile;
    internal string LockDir;

    public CmsIOSimpleFSLock(string lockDir, string lockFileName)
    {
        LockDir = lockDir;
        LockFile = CmsPath.Combine(lockDir, lockFileName);
    }

    public override bool Obtain()
    {
        // Ensure that lockDir exists and is a directory:
        if (!CmsDirectory.Exists(LockDir))
        {
            try
            {
                CmsDirectory.CreateDirectory(LockDir);
            }
            catch (Exception e)
            {
                throw new IOException($"Cannot create directory: {LockDir}", e);
            }
        }
        else if (CmsFile.Exists(LockDir)) // It's a file, not a directory
        {
            throw new IOException($"Found regular file where directory expected: {LockDir}");
        }

        // If the file already exists, we failed to obtain the lock
        if (CmsFile.Exists(LockFile))
        {
            FailureReason = new IOException($"lockFile '{LockFile}' already exists.");
            return false;
        }

        try
        {
            // Create the file
            CmsFile.WriteAllText(LockFile, string.Empty, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            return true;
        }
        catch (Exception e)
        {
            // On Windows, on concurrent createNewFile, the 2nd process gets "access denied".
            // In that case, the lock was not acquired successfully, so return false.
            // We record the failure reason here; the obtain with timeout (usually the
            // one calling us) will use this as "root cause" if it fails to get the lock.
            FailureReason = e;
            return false;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (CmsFile.Exists(LockFile))
            {
                try
                {
                    CmsFile.Delete(LockFile);
                }
                catch
                {
                    // Ignore
                }

                // If lockFile still exists, delete failed
                if (CmsFile.Exists(LockFile))
                {
                    throw new LockReleaseFailedException($"failed to delete {LockFile}");
                }
            }
        }
    }

    public override bool IsLocked()
        => CmsFile.Exists(LockFile);

    public override string ToString()
        => $"CmsIOSimpleFSLock@{LockFile}";
}
