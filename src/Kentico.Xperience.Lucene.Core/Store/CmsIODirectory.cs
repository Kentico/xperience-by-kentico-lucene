using Lucene.Net.Store;

using CmsDirectory = CMS.IO.Directory;
using CmsDirectoryInfo = CMS.IO.DirectoryInfo;
using CmsFile = CMS.IO.File;
using CmsFileInfo = CMS.IO.FileInfo;
using CmsPath = CMS.IO.Path;
using IOContext = Lucene.Net.Store.IOContext;

namespace Kentico.Xperience.Lucene.Core.Store;

/// <summary>
/// A Lucene Directory implementation that uses CMS.IO for all file operations.
/// This enables Lucene indexes to be stored on Azure Blob Storage or other custom
/// storage providers configured through Kentico's CMS.IO abstraction layer.
/// </summary>
internal class CmsIODirectory : BaseDirectory
{
    /// <summary>
    /// Opens a directory at the specified path for input/output operations using the default lock factory.
    /// </summary>
    /// <param name="path">The file system path to the directory to open. Cannot be null or empty.</param>
    /// <returns>A CmsIODirectory instance representing the opened directory at the specified path.</returns>
    public static CmsIODirectory Open(string path)
        => Open(path, NoOpLockFactory.Instance);


    /// <summary>
    /// Opens a directory at the specified path for input/output operations using provided lock factory.
    /// </summary>
    /// <param name="path">The path to the directory</param>
    /// <param name="lockFactory">The lock factory to use</param>
    /// <returns>A CmsIODirectory instance representing the opened directory at the specified path.</returns>
    public static CmsIODirectory Open(string path, LockFactory lockFactory)
    {
        return new CmsIODirectory(path, lockFactory);
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

        var freshInfo = CmsDirectoryInfo.New(DirectoryPath);
        var files = freshInfo.GetFiles();
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

        // For Azure Blob Storage (via CMS.IO), writes are already durable once the stream is closed.
        // We verify each file exists so that a failed write is caught before Lucene finalizes a commit
        // that references a missing file, which would result in a corrupted index.
        foreach (var name in names)
        {
            string filePath = GetFilePath(name);
            if (!CmsFile.Exists(filePath))
            {
                throw new IOException($"File \"{filePath}\" was not found during sync. The index may be corrupted.");
            }
        }
    }


    /// <summary>
    /// Closes the directory, releasing any resources.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        IsOpen = false;
    }


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

        // Strip all directory components to prevent path traversal attacks
        string safeName = Path.GetFileName(name);

        if (string.IsNullOrEmpty(safeName) || safeName != name)
        {
            throw new ArgumentException("File name cannot contain path separators or '..'.", nameof(name));
        }

        return CmsPath.Combine(DirectoryPath, safeName);
    }


    /// <summary>
    /// Ensures the directory exists, creating it if necessary.
    /// </summary>
    private void EnsureDirectoryExists()
    {
        if (!CmsDirectory.Exists(DirectoryPath))
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
