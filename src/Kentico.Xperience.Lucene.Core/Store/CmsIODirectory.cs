using Lucene.Net.Codecs;
using Lucene.Net.Store;
using Lucene.Net.Util;

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

namespace Kentico.Xperience.Lucene.Core.Store;

/// <summary>
/// A Lucene Directory implementation that uses CMS.IO for all file operations.
/// This enables Lucene indexes to be stored on Azure Blob Storage or other custom
/// storage providers configured through Kentico's CMS.IO abstraction layer.
/// </summary>
internal class CmsIODirectory : BaseDirectory
{
    // CMS.IO lowercases blob names on case-sensitive backends like Azure Blob Storage.
    // Lucene's per-field codec files embed the codec name in the filename with mixed casing,
    // e.g. _a_Lucene41_0.doc. When stored via CMS.IO these become _a_lucene41_0.doc.
    // We use the Lucene codec registries to build a case-insensitive lookup so ListAll()
    // can return the original-case names that Lucene embedded in the segments file.
    private static readonly Lazy<IReadOnlyDictionary<string, string>> codecNameLookup =
        new(BuildCodecNameLookup);


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
    /// Restores mixed-case codec names that were lowercased by the CMS.IO storage backend
    /// (e.g. Azure Blob Storage), so Lucene's IndexFileDeleter can match filenames against
    /// the names recorded in the segments file.
    /// </summary>
    public override string[] ListAll()
    {
        EnsureOpen();
        EnsureDirectoryExists();

        var freshInfo = CmsDirectoryInfo.New(DirectoryPath);
        return [.. freshInfo.GetFiles().Select(f => RestoreCodecNameCase(f.Name))];
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


    /// <summary>
    /// Restores the original mixed-case codec name in a filename that was lowercased by the
    /// storage backend. Lucene per-field codec files embed the codec or format name between
    /// underscores (e.g. <c>_a_Lucene41_0.doc</c>). This method splits on <c>_</c> and maps
    /// any segment that matches a registered codec name back to its canonical casing.
    /// </summary>
    private static string RestoreCodecNameCase(string fileName)
    {
        var lookup = codecNameLookup.Value;
        var parts = fileName.Split('_');

        for (int i = 0; i < parts.Length; i++)
        {
            if (lookup.TryGetValue(parts[i], out string? original))
            {
                parts[i] = original;
            }
        }

        return string.Join('_', parts);
    }


    /// <summary>
    /// Builds a case-insensitive lookup from lowercase codec/format name to the original-case
    /// name as registered in Lucene's PostingsFormat, DocValuesFormat and Codec factories.
    /// </summary>
    private static Dictionary<string, string> BuildCodecNameLookup()
    {
        var lookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (PostingsFormat.GetPostingsFormatFactory() is IServiceListable postingsListable)
        {
            foreach (string name in postingsListable.AvailableServices)
            {
                lookup[name] = name;
            }
        }

        if (DocValuesFormat.GetDocValuesFormatFactory() is IServiceListable docValuesListable)
        {
            foreach (string name in docValuesListable.AvailableServices)
            {
                lookup[name] = name;
            }
        }

        if (Codec.GetCodecFactory() is IServiceListable codecListable)
        {
            foreach (string name in codecListable.AvailableServices)
            {
                lookup[name] = name;
            }
        }

        return lookup;
    }
}

