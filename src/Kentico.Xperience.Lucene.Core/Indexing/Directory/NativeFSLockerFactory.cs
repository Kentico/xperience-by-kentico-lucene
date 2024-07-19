using CMS.IO;

using Lucene.Net.Store;
using Lucene.Net.Support.IO;
using Lucene.Net.Util;

using Directory = CMS.IO.Directory;
using DirectoryInfo = CMS.IO.DirectoryInfo;
using File = CMS.IO.File;
using FileAccess = CMS.IO.FileAccess;
using FileInfo = CMS.IO.FileInfo;
using FileMode = CMS.IO.FileMode;
using FileShare = CMS.IO.FileShare;
using FileStream = CMS.IO.FileStream;
using Path = CMS.IO.Path;

namespace Kentico.Xperience.Lucene.Core.Indexing;
public class KenticoFSLockFactory : FSLockFactory
{
    internal enum FSLockingStrategy
    {
        FileStreamLockViolation,
        FileSharingViolation,
        Fallback
    }

    internal static FSLockingStrategy LockingStrategy
    {
        get
        {
            if (iS_FILESTREAM_LOCKING_PLATFORM && HRESULT_FILE_LOCK_VIOLATION.HasValue)
            {
                return FSLockingStrategy.FileStreamLockViolation;
            }
            else if (HRESULT_FILE_SHARE_VIOLATION.HasValue)
            {
                return FSLockingStrategy.FileSharingViolation;
            }
            else
            {
                return FSLockingStrategy.Fallback;
            }
        }
    }

    private static readonly bool iS_FILESTREAM_LOCKING_PLATFORM = LoadIsFileStreamLockingPlatform();

    private const int WIN_HRESULT_FILE_LOCK_VIOLATION = unchecked((int)0x80070021);
    private const int WIN_HRESULT_FILE_SHARE_VIOLATION = unchecked((int)0x80070020);

    internal static readonly int? HRESULT_FILE_LOCK_VIOLATION = LoadFileLockViolationHResult();
    internal static readonly int? HRESULT_FILE_SHARE_VIOLATION = LoadFileShareViolationHResult();

    private static bool LoadIsFileStreamLockingPlatform() =>
#if FEATURE_FILESTREAM_LOCK
        return Constants.WINDOWS; // LUCENENET: See: https://github.com/dotnet/corefx/issues/5964
#else
        false;
#endif


    private static int? LoadFileLockViolationHResult()
    {
        if (Constants.WINDOWS)
        {
            return WIN_HRESULT_FILE_LOCK_VIOLATION;
        }

        if (iS_FILESTREAM_LOCKING_PLATFORM)
        {
            using var lockStream = FileStream.New(fileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
#pragma warning disable CA1416
            lockStream.Lock(0, 1);
#pragma warning restore CA1416 // Validate platform compatibility
            using var stream = new FileStream(fileName, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
            stream.WriteByte(0);
            stream.Flush();   // this *may* throw an IOException if the file is locked, but...
        }

        return null;
    }

    private static int? LoadFileShareViolationHResult()
    {
        if (Constants.WINDOWS)
        {
            return WIN_HRESULT_FILE_SHARE_VIOLATION;
        }

        return FileSupport.GetFileIOExceptionHResult(provokeException: (fileName) =>
        {
            using var lockStream = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read, 1, FileOptions.None);
            using var stream = new FileStream(fileName, FileMode.Open, FileAccess.Write, FileShare.None, 1, FileOptions.None);
        });
    }

    public KenticoFSLockFactory()
        : this(null)
    {
    }

    public KenticoFSLockFactory(string lockDirName)
        : this(DirectoryInfo.New(lockDirName))
    {
    }

    public KenticoFSLockFactory(DirectoryInfo lockDir) => SetLockDir(lockDir);

    internal static readonly Dictionary<string, Lock> LocksInternal = [];

    private string GetCanonicalPathOfLockFile(string lockName)
    {
        if (m_lockPrefix != null)
        {
            lockName = m_lockPrefix + "-" + lockName;
        }
        return FileInfo.New(Path.Combine(LockDirInternal!.FullName, lockName)).GetCanonicalPath();
    }

    public override Lock MakeLock(string lockName)
    {
        string path = GetCanonicalPathOfLockFile(lockName);
        Lock l;
        UninterruptableMonitor.Enter(LocksInternal);
        try
        {
            if (!LocksInternal.TryGetValue(path, out l))
            {
                LocksInternal.Add(path, l = NewLock(path));
            }
        }
        finally
        {
            UninterruptableMonitor.Exit(LocksInternal);
        }
        return l;
    }

#pragma warning disable IDE0072
    internal virtual Lock NewLock(string path) => LockingStrategy switch
    {
        FSLockingStrategy.FileStreamLockViolation => new NativeFSLock(LockDirInternal!, path),
        FSLockingStrategy.FileSharingViolation => new SharingNativeFSLock(LockDirInternal!, path),
        _ => new FallbackNativeFSLock(LockDirInternal!, path),
    };
#pragma warning restore IDE0072

    public override void ClearLock(string lockName)
    {
        var path = GetCanonicalPathOfLockFile(lockName);
        UninterruptableMonitor.Enter(LocksInternal);
        try
        {
            if (LocksInternal.TryGetValue(path, out var l))
            {
                LocksInternal.Remove(path);
                l.Dispose();
            }
        }
        finally
        {
            UninterruptableMonitor.Exit(LocksInternal);
        }
    }
}

internal class FallbackNativeFSLock : Lock
{
    private FileStream? channel;
    private readonly string path;
    private readonly DirectoryInfo lockDir;

    public FallbackNativeFSLock(DirectoryInfo lockDir, string path)
    {
        this.lockDir = lockDir;
        this.path = path;
    }

    public override bool Obtain()
    {
        UninterruptableMonitor.Enter(this);
        try
        {
            FailureReason = null;

            if (channel != null)
            {
                return false;
            }

            if (!Directory.Exists(lockDir.FullName))
            {
                try
                {
                    Directory.CreateDirectory(lockDir.FullName);
                }
                catch
                {
                    throw new IOException("Cannot create directory: " + lockDir.FullName);
                }
            }
            else if (File.Exists(lockDir.FullName))
            {
                throw new IOException("Found regular file where directory expected: " + lockDir.FullName);
            }

            bool success = false;
            try
            {
                // LUCENENET: Allow read access for the RAMDirectory to be able to copy the lock file.
                channel = FileStream.New(path, FileMode.Create, FileAccess.Write, FileShare.Read);

                success = true;
            }
            catch (Exception e) when (IsIOException(e))
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
        finally
        {
            UninterruptableMonitor.Exit(this);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                try
                {
                    UninterruptableMonitor.Enter(KenticoFSLockFactory.LocksInternal);
                    try
                    {
                        KenticoFSLockFactory.LocksInternal.Remove(path);
                    }
                    finally
                    {
                        UninterruptableMonitor.Exit(KenticoFSLockFactory.LocksInternal);
                    }
                }
                finally
                {
                    if (channel != null)
                    {
                        IOUtils.DisposeWhileHandlingException(channel);
                        channel = null;

                        bool tmpBool;
                        if (File.Exists(path))
                        {
                            File.Delete(path);
                            tmpBool = true;
                        }
                        else if (Directory.Exists(path))
                        {
                            Directory.Delete(path);
                            tmpBool = true;
                        }
                        else
                        {
                            tmpBool = false;
                        }
                        if (!tmpBool)
                        {
                            throw new LockReleaseFailedException("failed to delete " + path);
                        }
                    }
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }
    }

    public override bool IsLocked()
    {
        UninterruptableMonitor.Enter(this);
        try
        {
            if (channel != null)
            {
                return true;
            }

            bool tmpBool = File.Exists(path) ? true : Directory.Exists(path);

            if (!tmpBool)
            {
                return false;
            }

            try
            {
                bool obtained = Obtain();
                if (obtained)
                {
                    Dispose();
                }
                return !obtained;
            }
            catch (Exception ioe) when (IsIOException(ioe))
            {
                return false;
            }
        }
        finally
        {
            UninterruptableMonitor.Exit(this);
        }
    }

    public override string ToString() => $"{nameof(FallbackNativeFSLock)}@{path}";

    public bool IsIOException(Exception e)
    {
        if (e is null)
        {
            return false;
        }

        return e is IOException or
            ObjectDisposedException or
            UnauthorizedAccessException;
    }
}

internal class SharingNativeFSLock : Lock
{
#pragma warning disable CA2213 // Disposable fields should be disposed
    private FileStream? channel;
#pragma warning restore CA2213 // Disposable fields should be disposed
    private readonly string path;
    private readonly DirectoryInfo lockDir;

    public SharingNativeFSLock(DirectoryInfo lockDir, string path)
    {
        this.lockDir = lockDir;
        this.path = path;
    }

    private static bool IsShareViolation(IOException e) // LUCENENET: CA1822: Mark members as static
        => e.HResult == KenticoFSLockFactory.HRESULT_FILE_SHARE_VIOLATION;

    private FileStream GetLockFileStream(FileMode mode)
    {
        if (!Directory.Exists(lockDir.FullName))
        {
            try
            {
                Directory.CreateDirectory(lockDir.FullName);
            }
            catch (Exception e)
            {
                throw new IOException("Cannot create directory: " + lockDir.FullName, e);
            }
        }
        else if (File.Exists(lockDir.FullName))
        {
            throw new IOException("Found regular file where directory expected: " + lockDir.FullName);
        }

        return FileStream.New(
            path,
            mode,
            FileAccess.Write,
            share: mode == FileMode.Open ? FileShare.None : FileShare.Read,
            bufferSize: 1);
    }

    public override bool Obtain()
    {
        UninterruptableMonitor.Enter(this);
        try
        {
            FailureReason = null;

            if (channel != null)
            {
                // Our instance is already locked:
                return false;
            }
            try
            {
                channel = GetLockFileStream(FileMode.OpenOrCreate);
            }
            catch (IOException e) when (IsShareViolation(e))
            {
                // no failure reason to be recorded, since this is the expected error if a lock exists
            }
            catch (Exception e) when (IsIOException(e))
            {
                FailureReason = e;
            }
            return channel != null;
        }
        finally
        {
            UninterruptableMonitor.Exit(this);
        }
    }

    public bool IsIOException(Exception e)
    {
        if (e is null)
        {
            return false;
        }

        return e is IOException or
            ObjectDisposedException or
            UnauthorizedAccessException;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                try
                {
                    UninterruptableMonitor.Enter(KenticoFSLockFactory.LocksInternal);
                    try
                    {
                        KenticoFSLockFactory.LocksInternal.Remove(path);
                    }
                    finally
                    {
                        UninterruptableMonitor.Exit(KenticoFSLockFactory.LocksInternal);
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
                    }
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }
    }

    public override bool IsLocked()
    {
        UninterruptableMonitor.Enter(this);
        try
        {
            if (channel != null)
            {
                return true;
            }

            try
            {
                using var stream = GetLockFileStream(FileMode.Open);
                return false;
            }
            catch (IOException e) when (IsShareViolation(e))
            {
                return true;
            }
            catch (FileNotFoundException)
            {
                return false;
            }
        }
        finally
        {
            UninterruptableMonitor.Exit(this);
        }
    }

    public override string ToString() => $"{nameof(SharingNativeFSLock)}@{path}";
}

#if NET6_0
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
#endif
internal class NativeFSLock : Lock
{
#pragma warning disable CA2213 // Disposable fields should be disposed
    private FileStream? channel;
#pragma warning restore CA2213 // Disposable fields should be disposed
    private readonly string path;
    private readonly DirectoryInfo lockDir;

    public NativeFSLock(DirectoryInfo lockDir, string path)
    {
        this.lockDir = lockDir;
        this.path = path;
    }

    private static bool IsLockViolation(IOException e) // LUCENENET: CA1822: Mark members as static
=> e.HResult == KenticoFSLockFactory.HRESULT_FILE_LOCK_VIOLATION;

    private FileStream GetLockFileStream(FileMode mode)
    {
        if (!Directory.Exists(lockDir.FullName))
        {
            try
            {
                Directory.CreateDirectory(lockDir.FullName);
            }
            catch (Exception e)
            {
                if (!Directory.Exists(lockDir.FullName))
                {
                    throw new IOException("Cannot create directory: " + lockDir.FullName, e);
                }
            }
        }
        else if (File.Exists(lockDir.FullName))
        {
            throw new IOException("Found regular file where directory expected: " + lockDir.FullName);
        }

        return FileStream.New(path, mode, FileAccess.Write, FileShare.ReadWrite);
    }

    public bool IsIOException(Exception e)
    {
        if (e is null)
        {
            return false;
        }

        return e is IOException or
            ObjectDisposedException or
            UnauthorizedAccessException;
    }

    public override bool Obtain()
    {
        UninterruptableMonitor.Enter(this);
        try
        {
            FailureReason = null;

            if (channel != null)
            {
                // Our instance is already locked:
                return false;
            }

            FileStream? stream = null;
            try
            {
                stream = GetLockFileStream(FileMode.OpenOrCreate);
            }
            catch (Exception e) when (IsIOException(e))
            {
                FailureReason = e;
            }

            if (stream != null)
            {
                try
                {
                    stream.Lock(0, 1);
                    channel = stream;
                }
                catch (Exception e)
                {
                    FailureReason = e;
                    IOUtils.DisposeWhileHandlingException(stream);
                }
            }
            return channel != null;
        }
        finally
        {
            UninterruptableMonitor.Exit(this);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                try
                {
                    UninterruptableMonitor.Enter(KenticoFSLockFactory.LocksInternal);
                    try
                    {
                        KenticoFSLockFactory.LocksInternal.Remove(path);
                    }
                    finally
                    {
                        UninterruptableMonitor.Exit(KenticoFSLockFactory.LocksInternal);
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
                        try
                        {
                            File.Delete(path);
                        }
                        catch
                        {
                        }
                    }
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }
    }

    public override bool IsLocked()
    {
        UninterruptableMonitor.Enter(this);
        try
        {
            if (channel != null)
            {
                return true;
            }

            try
            {
                using var stream = GetLockFileStream(FileMode.Open);
                stream.WriteByte(0);
                stream.Flush();
                return false;
            }
            catch (IOException e) when (IsLockViolation(e))
            {
                return true;
            }
            catch (FileNotFoundException)
            {
                return false;
            }
        }
        finally
        {
            UninterruptableMonitor.Exit(this);
        }
    }

    public override string ToString() => $"{nameof(NativeFSLock)}@{path}";
}


public abstract class FSLockFactory : LockFactory
{
    protected internal DirectoryInfo? LockDirInternal = null;

    protected internal void SetLockDir(DirectoryInfo lockDir)
    {
        if (LockDirInternal != null)
        {
            throw new InvalidOperationException("You can set the lock directory for this factory only once.");
        }
        LockDirInternal = lockDir;
    }

    public DirectoryInfo LockDir => LockDirInternal!;

    public override string ToString() => GetType().Name + "@" + LockDirInternal;
}
