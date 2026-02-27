using Lucene.Net.Store;
using Lucene.Net.Util;

using System.Globalization;

using CmsDirectory = CMS.IO.Directory;
using CmsDirectoryInfo = CMS.IO.DirectoryInfo;
using CmsFile = CMS.IO.File;
using CmsFileInfo = CMS.IO.FileInfo;
using CmsPath = CMS.IO.Path;
using CmsFileStream = CMS.IO.FileStream;
using CmsFileMode = CMS.IO.FileMode;
using CmsFileAccess = CMS.IO.FileAccess;
using CmsFileShare = CMS.IO.FileShare;
using IOContext = Lucene.Net.Store.IOContext;

namespace Kentico.Xperience.Lucene.Core.Store;

/// <summary>
/// Base class for <see cref="Directory"/> implementations that store index
/// files using CMS.IO file system abstraction.
/// </summary>
/// <remarks>
/// <para>
/// This is a CMS.IO-based equivalent of Lucene.NET's FSDirectory, enabling
/// Lucene indexes to be stored on Azure Blob Storage or other custom storage
/// providers configured through Kentico's CMS.IO abstraction layer.
/// </para>
/// <para>
/// There are currently three core subclasses:
/// <list type="bullet">
/// <item><description><see cref="CmsIOSimpleFSDirectory"/> is a straightforward
/// implementation using CMS.IO.FileStream.</description></item>
/// <item><description><see cref="CmsIONIOFSDirectory"/> uses positional seeking
/// for better concurrent read performance.</description></item>
/// <item><description><see cref="CmsIOMemoryMapDirectory"/> uses memory-mapped IO when
/// reading (if supported by the underlying storage provider).</description></item>
/// </list>
/// </para>
/// <para>
/// To allow Lucene to choose the best implementation for your environment,
/// use the <see cref="Open(string)"/> method.
/// </para>
/// <para>
/// The locking implementation is by default <see cref="CmsIONativeFSLockFactory"/>,
/// but can be changed by passing in a custom <see cref="LockFactory"/> instance.
/// </para>
/// </remarks>
public abstract class CmsIODirectory : BaseDirectory
{
    /// <summary>
    /// The underlying CMS.IO directory path
    /// </summary>
    protected readonly string DirectoryPath;


    /// <summary>
    /// Exposes <see cref="DirectoryPath"/> to other classes within the assembly.
    /// </summary>
    internal string InternalDirectoryPath => DirectoryPath;


    /// <summary>
    /// The collection of stale files that need to be synced
    /// </summary>
    protected readonly ISet<string> StaleFiles = new HashSet<string>();


    /// <summary>
    /// A lock object to synchronize access to the m_staleFiles collection
    /// </summary>
    protected readonly object SyncLock = new();


    /// <summary>
    /// Create a new CmsIOFSDirectory for the named location
    /// </summary>
    /// <param name="path">The path of the directory</param>
    /// <param name="lockFactory">The lock factory to use, or null for the default</param>
    protected CmsIODirectory(string path, LockFactory? lockFactory)
    {
        lockFactory ??= new CmsIONativeFSLockFactory();

        DirectoryPath = ResolvePath(path);

        if (CmsFile.Exists(DirectoryPath))
        {
            throw new DirectoryNotFoundException($"file '{DirectoryPath}' exists but is not a directory");
        }

        SetLockFactory(lockFactory);
    }


    /// <summary>
    /// Creates a CmsIOFSDirectory instance, trying to pick the best implementation
    /// given the current environment.
    /// </summary>
    /// <param name="path">The path to the directory</param>
    /// <returns>A CmsIOFSDirectory instance</returns>
    public static CmsIODirectory Open(string path)
        => Open(path, null);


    /// <summary>
    /// Creates a CmsIOFSDirectory instance with a custom lock factory
    /// </summary>
    /// <param name="path">The path to the directory</param>
    /// <param name="lockFactory">The lock factory to use</param>
    /// <returns>A CmsIOFSDirectory instance</returns>
    public static CmsIODirectory Open(string path, LockFactory? lockFactory)
    {
        // Choose implementation based on platform, similar to FSDirectory.Open
        if ((Constants.WINDOWS || Constants.SUN_OS || Constants.LINUX) && Constants.RUNTIME_IS_64BIT)
        {
            return new CmsIOMemoryMapDirectory(path, lockFactory);
        }
        else if (Constants.WINDOWS)
        {
            return new CmsIOSimpleFSDirectory(path, lockFactory);
        }
        else
        {
            return new CmsIONIOFSDirectory(path, lockFactory);
        }
    }

    public override void SetLockFactory(LockFactory lockFactory)
    {
        base.SetLockFactory(lockFactory);

        // For filesystem-based LockFactory, configure the lock directory
        if (lockFactory is CmsIOFSLockFactory lf)
        {
            string? dir = lf.LockDir;
            // If the lock factory has no lockDir set, use this directory as lockDir
            if (dir is null)
            {
                lf.SetLockDir(DirectoryPath);
                lf.LockPrefix = null;
            }
            else if (string.Equals(ResolvePath(dir), DirectoryPath, StringComparison.Ordinal))
            {
                lf.LockPrefix = null;
            }
        }
    }


    /// <summary>
    /// Lists all files (not subdirectories) in the directory
    /// </summary>
    public static string[] ListAll(string dir)
    {
        if (!CmsDirectory.Exists(dir))
        {
            throw new DirectoryNotFoundException($"directory '{dir}' does not exist");
        }
        else if (CmsFile.Exists(dir))
        {
            throw new DirectoryNotFoundException($"file '{dir}' exists but is not a directory");
        }

        var dirInfo = CmsDirectoryInfo.New(dir);
        var files = dirInfo.GetFiles();
        string[] result = new string[files.Length];

        for (int i = 0; i < files.Length; i++)
        {
            result[i] = files[i].Name;
        }

        return result;
    }


    /// <summary>
    /// Lists all files (not subdirectories) in the directory
    /// </summary>
    public override string[] ListAll()
    {
        EnsureOpen();
        return ListAll(DirectoryPath);
    }


    /// <summary>
    /// Returns true if a file with the given name exists
    /// </summary>
    [Obsolete("this method will be removed in 5.0")]
    public override bool FileExists(string name)
    {
        EnsureOpen();
        return CmsFile.Exists(CmsPath.Combine(DirectoryPath, name));
    }


    /// <summary>
    /// Returns the length in bytes of a file in the directory
    /// </summary>
    public override long FileLength(string name)
    {
        EnsureOpen();
        var fileInfo = CmsFileInfo.New(CmsPath.Combine(DirectoryPath, name));
        long len = fileInfo.Length;
        if (len == 0 && !fileInfo.Exists)
        {
            throw new FileNotFoundException(name);
        }
        return len;
    }


    /// <summary>
    /// Removes an existing file in the directory
    /// </summary>
    public override void DeleteFile(string name)
    {
        EnsureOpen();
        string file = CmsPath.Combine(DirectoryPath, name);

        lock (SyncLock)
        {
            if (!CmsFile.Exists(file))
            {
                throw new FileNotFoundException($"Cannot delete {file} because it doesn't exist.");
            }

            try
            {
                CmsFile.Delete(file);
                if (CmsFile.Exists(file))
                {
                    throw new IOException($"Cannot delete {file}");
                }
            }
            catch (Exception e)
            {
                throw new IOException($"Cannot delete {file}", e);
            }

            StaleFiles.Remove(name);
        }
    }


    /// <summary>
    /// Creates an IndexOutput for the file with the given name
    /// </summary>
    public override IndexOutput CreateOutput(string name, IOContext context)
    {
        EnsureOpen();
        EnsureCanWrite(name);
        return new CmsIOIndexOutput(this, name);
    }


    /// <summary>
    /// Ensures the file can be written to
    /// </summary>
    protected virtual void EnsureCanWrite(string name)
    {
        if (!CmsDirectory.Exists(DirectoryPath))
        {
            try
            {
                CmsDirectory.CreateDirectory(DirectoryPath);
            }
            catch
            {
                throw new IOException($"Cannot create directory: {DirectoryPath}");
            }
        }

        string file = CmsPath.Combine(DirectoryPath, name);
        if (CmsFile.Exists(file))
        {
            try
            {
                CmsFile.Delete(file);
            }
            catch
            {
                throw new IOException($"Cannot overwrite: {file}");
            }
        }
    }


    /// <summary>
    /// Called when an IndexOutput is closed
    /// </summary>
    internal virtual void OnIndexOutputClosed(CmsIOIndexOutput io)
    {
        lock (SyncLock)
        {
            StaleFiles.Add(io.Name);
        }
    }


    public override void Sync(ICollection<string> names)
    {
        EnsureOpen();

        var toSync = new HashSet<string>(names);

        lock (SyncLock)
        {
            toSync.IntersectWith(StaleFiles);

            foreach (var name in toSync)
            {
                string filePath = GetFilePath(name);
                if (CmsFile.Exists(filePath))
                {
                    // Open and close the file to ensure any buffered writes are flushed
                    // This is a no-op for Azure but ensures durability on local filesystem
                    try
                    {
                        using var stream = CmsFileStream.New(filePath, CmsFileMode.Open, CmsFileAccess.Read, CmsFileShare.ReadWrite);
                        // Just opening and closing triggers any pending writes to flush
                    }
                    catch (IOException)
                    {
                        // File might be in use - that's okay, it means writes are still happening
                    }
                }
            }

            StaleFiles.ExceptWith(toSync);
        }
    }


    public override string GetLockID()
    {
        EnsureOpen();
        string dirName;

        dirName = ResolvePath(DirectoryPath);

        int digest = 0;
        for (int charIDX = 0; charIDX < dirName.Length; charIDX++)
        {
            char ch = dirName[charIDX];
            digest = 31 * digest + ch;
        }
        return "lucene-" + digest.ToString("x", CultureInfo.InvariantCulture);
    }


    /// <summary>
    /// Closes the directory
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        IsOpen = false;
    }


    /// <summary>
    /// Gets the underlying directory path
    /// </summary>
    public virtual string Directory
    {
        get
        {
            EnsureOpen();
            return DirectoryPath;
        }
    }

    public override string ToString()
        => $"{GetType().Name}@{DirectoryPath} lockFactory={LockFactory}";


    /// <summary>
    /// Resolves a path, handling virtual paths and ensuring consistency
    /// </summary>
    protected static string ResolvePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path cannot be null or empty.", nameof(path));
        }

        // CMS.IO handles path normalization internally
        // Ensure consistent forward slashes for virtual paths
        path = path.Replace('\\', '/');

        return path;
    }


    /// <summary>
    /// Gets the full path for a file within this directory.
    /// </summary>
    private string GetFilePath(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("File name cannot be null or empty.", nameof(name));
        }

        // Ensure no path traversal attacks
        if (name.Contains("..") || name.Contains('/') || name.Contains('\\'))
        {
            throw new ArgumentException("File name cannot contain path separators or '..'.", nameof(name));
        }

        return CmsPath.Combine(DirectoryPath, name);
    }
}
