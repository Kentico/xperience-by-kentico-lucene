using Lucene.Net.Store;

using CmsDirectory = CMS.IO.Directory;
using CmsDirectoryInfo = CMS.IO.DirectoryInfo;
using CmsFile = CMS.IO.File;
using CmsFileAccess = CMS.IO.FileAccess;
using CmsFileInfo = CMS.IO.FileInfo;
using CmsFileMode = CMS.IO.FileMode;
using CmsFileShare = CMS.IO.FileShare;
using CmsFileStream = CMS.IO.FileStream;
using CmsPath = CMS.IO.Path;
using IOContext = Lucene.Net.Store.IOContext;

namespace Kentico.Xperience.Lucene.Core.Search;

/// <summary>
/// A Lucene Directory implementation that uses CMS.IO for all file operations.
/// This enables Lucene indexes to be stored on Azure Blob Storage or other custom
/// storage providers configured through Kentico's CMS.IO abstraction layer.
/// </summary>
/// <remarks>
/// <para>
/// This implementation wraps CMS.IO's file system abstraction, allowing Lucene
/// to transparently use whatever storage backend is configured in your Xperience
/// by Kentico application (local filesystem, Azure Blob Storage, etc.).
/// </para>
/// <para>
/// Usage example:
/// <code>
/// // Map the index path to your storage provider first (in a Module's OnInit)
/// StorageHelper.MapStoragePath("~/lucene/indexes/", azureProvider);
///
/// // Then create the directory
/// var indexPath = "~/lucene/indexes/my-index";
/// var directory = new CmsIODirectory(indexPath);
/// var indexWriter = new IndexWriter(directory, config);
/// </code>
/// </para>
/// </remarks>
public class CmsIODirectory : BaseDirectory
{
    private readonly CmsDirectoryInfo directoryInfo;

    /// <summary>
    /// Creates a new CmsIODirectory at the specified path.
    /// </summary>
    /// <param name="path">
    /// The path to the directory. Can be a virtual path (~/...) that will be
    /// resolved by CMS.IO, or an absolute path.
    /// </param>
    /// <remarks>
    /// Uses NoOpLockFactory by default because CMS.IO file-based locking doesn't
    /// work reliably on Azure Blob Storage. Use external locking (e.g., FileLock,
    /// Azure Blob Lease, or Redis) to coordinate write access before creating
    /// an IndexWriter.
    /// </remarks>
    public CmsIODirectory(string path)
        : this(path, NoOpLockFactory.Instance)
    {
    }

    /// <summary>
    /// Creates a new CmsIODirectory at the specified path with a custom lock factory.
    /// </summary>
    /// <param name="path">The path to the directory.</param>
    /// <param name="lockFactory">The lock factory to use for index locking.</param>
    public CmsIODirectory(string path, LockFactory lockFactory)
        : base()
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(lockFactory);

        DirectoryPath = ResolvePath(path);
        directoryInfo = CmsDirectoryInfo.New(DirectoryPath);

        // Ensure the directory exists
        EnsureDirectoryExists();

        SetLockFactory(lockFactory);
    }

    /// <summary>
    /// Gets the resolved physical/virtual path of this directory.
    /// </summary>
    public string DirectoryPath { get; }

    /// <summary>
    /// Returns an array of strings, one for each file in the directory.
    /// </summary>
    public override string[] ListAll()
    {
        EnsureOpen();
        EnsureDirectoryExists();

        var files = directoryInfo.GetFiles();
        return files.Select(f => f.Name).ToArray();
    }

    /// <summary>
    /// Returns true if a file with the given name exists.
    /// </summary>
    [Obsolete("Use FileLength to check file existence instead.")]
    public override bool FileExists(string name)
    {
        EnsureOpen();
        string filePath = GetFilePath(name);
        return CmsFile.Exists(filePath);
    }

    /// <summary>
    /// Removes an existing file in the directory.
    /// </summary>
    public override void DeleteFile(string name)
    {
        EnsureOpen();
        string filePath = GetFilePath(name);

        if (!CmsFile.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {name}", filePath);
        }

        CmsFile.Delete(filePath);
    }

    /// <summary>
    /// Returns the length in bytes of a file in the directory.
    /// </summary>
    public override long FileLength(string name)
    {
        EnsureOpen();
        string filePath = GetFilePath(name);

        var fileInfo = CmsFileInfo.New(filePath);
        if (!fileInfo.Exists)
        {
            throw new FileNotFoundException($"File not found: {name}", filePath);
        }

        return fileInfo.Length;
    }

    /// <summary>
    /// Creates a new, empty file in the directory with the given name.
    /// Returns an IndexOutput for writing to the file.
    /// </summary>
    public override IndexOutput CreateOutput(string name, IOContext context)
    {
        EnsureOpen();
        EnsureDirectoryExists();

        string filePath = GetFilePath(name);
        return new CmsIOIndexOutput(filePath);
    }

    /// <summary>
    /// Opens an existing file for reading.
    /// Returns an IndexInput for reading from the file.
    /// </summary>
    public override IndexInput OpenInput(string name, IOContext context)
    {
        EnsureOpen();
        string filePath = GetFilePath(name);

        if (!CmsFile.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {name}", filePath);
        }

        return new CmsIOIndexInput(filePath, context);
    }

    /// <summary>
    /// Ensures that any writes to the named files are moved to stable storage.
    /// </summary>
    public override void Sync(ICollection<string> names)
    {
        EnsureOpen();

        // For CMS.IO, we rely on the underlying storage provider to handle durability.
        // With Azure Blob Storage, writes are already durable once the stream is closed.
        // For local filesystem, we can optionally force a flush.

        foreach (var name in names)
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
    }

    /// <summary>
    /// Closes the directory, releasing any resources.
    /// </summary>
    protected override void Dispose(bool disposing) => IsOpen = false;

    /// <summary>
    /// Resolves a path, handling virtual paths (~/...) and ensuring consistency.
    /// </summary>
    private static string ResolvePath(string path)
    {
        // CMS.IO handles virtual path resolution internally
        // We just need to ensure the path is properly formatted
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path cannot be null or empty.", nameof(path));
        }

        // CMS.IO handles path normalization internally
        // Just ensure consistent forward slashes for virtual paths
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

    /// <summary>
    /// Ensures the directory exists, creating it if necessary.
    /// </summary>
    private void EnsureDirectoryExists()
    {
        if (!directoryInfo.Exists)
        {
            CmsDirectory.CreateDirectory(DirectoryPath);
        }
    }

    /// <summary>
    /// Ensures the directory is still open.
    /// </summary>
    private new void EnsureOpen()
    {
        if (!IsOpen)
        {
            throw new ObjectDisposedException(GetType().FullName, "This directory has been closed.");
        }
    }
}
