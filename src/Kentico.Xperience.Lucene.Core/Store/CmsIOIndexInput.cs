using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Lucene.Net.Store;

using static Lucene.Net.Util.OfflineSorter;

using IOContext = Lucene.Net.Store.IOContext;
using CmsFileStream = CMS.IO.FileStream;
using CmsFileMode = CMS.IO.FileMode;
using CmsFileAccess = CMS.IO.FileAccess;
using CmsFileShare = CMS.IO.FileShare;

namespace Kentico.Xperience.Lucene.Core.Store;

/// <summary>
/// A Lucene IndexInput implementation that reads from CMS.IO FileStream.
/// This allows Lucene to read index files through the CMS.IO abstraction layer,
/// enabling storage on Azure Blob Storage or other custom storage providers.
/// </summary>
public class CmsIOIndexInput : BufferedIndexInput
{
    private readonly string path;
    private CmsFileStream? stream;
    private readonly long length;
    private bool isClone;
    private bool isDisposed;

    /// <summary>
    /// Creates a new CmsIOIndexInput for reading the specified file.
    /// </summary>
    /// <param name="path">The full path to the file to read.</param>
    /// <param name="context">The IO context for buffer size hints.</param>
    public CmsIOIndexInput(string path, IOContext context)
        : base($"CmsIOIndexInput(path=\"{path}\")", DetermineBufferSize(context))
    {
        this.path = path;
        this.stream = CmsFileStream.New(path, CmsFileMode.Open, CmsFileAccess.Read, CmsFileShare.ReadWrite);
        this.length = stream.Length;
        this.isClone = false;
    }

    /// <summary>
    /// Private constructor for cloning.
    /// </summary>
    private CmsIOIndexInput(string resourceDescription, string path, CmsFileStream stream, long length, int bufferSize)
        : base(resourceDescription, bufferSize)
    {
        this.path = path;
        this.stream = stream;
        this.length = length;
        this.isClone = true;
    }

    /// <summary>
    /// Gets the total length of the file in bytes.
    /// </summary>
    public override long Length => length;

    /// <summary>
    /// Reads bytes from the file into the buffer at the specified position.
    /// This is the core read method called by BufferedIndexInput.
    /// </summary>
    protected override void ReadInternal(byte[] b, int offset, int len)
    {
        if (isDisposed || stream == null)
        {
            throw new ObjectDisposedException(GetType().FullName);
        }

        lock (stream)
        {
            long position = Position; // Use Position property from BufferedIndexInput
            if (position != stream.Position)
            {
                stream.Seek(position, System.IO.SeekOrigin.Begin);
            }

            int totalRead = 0;
            while (totalRead < len)
            {
                int bytesRead = stream.Read(b, offset + totalRead, len - totalRead);
                if (bytesRead == 0)
                {
                    throw new System.IO.EndOfStreamException($"Read past EOF: {this}");
                }
                totalRead += bytesRead;
            }
        }
    }

    /// <summary>
    /// Seeks to a position in the file. BufferedIndexInput handles buffering,
    /// so this just needs to track the logical position.
    /// </summary>
    protected override void SeekInternal(long pos)
    {
        // BufferedIndexInput handles the actual seeking through ReadInternal
        // We just need to ensure the position is valid
        if (pos < 0 || pos > length)
        {
            throw new ArgumentOutOfRangeException(nameof(pos),
                $"Seek position {pos} is out of range [0, {length}]");
        }
    }

    /// <summary>
    /// Creates a clone of this IndexInput that can be used from another thread.
    /// The clone operates on the same underlying file but maintains its own position.
    /// </summary>
    public override object Clone()
    {
        var clone = (CmsIOIndexInput)base.Clone();

        // Create a new stream for the clone to allow independent positioning
        var cloneStream = CmsFileStream.New(path, CmsFileMode.Open, CmsFileAccess.Read, CmsFileShare.ReadWrite);

        return new CmsIOIndexInput(
            $"CmsIOIndexInput(path=\"{path}\") [clone]",
            path,
            cloneStream,
            length,
            BufferSize)
        {
            isClone = true
        };
    }

    /// <summary>
    /// Disposes of the underlying stream resources.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (isDisposed)
        {
            return;
        }

        if (disposing)
        {
            try
            {
                stream?.Dispose();
            }
            finally
            {
                stream = null;
                isDisposed = true;
            }
        }
    }

    /// <summary>
    /// Determines the appropriate buffer size based on the IOContext.
    /// Larger buffers reduce the number of read operations, which is important
    /// for CMS.IO with Azure Blob Storage where each read may trigger cache validation.
    /// </summary>
    private static int DetermineBufferSize(IOContext context)
    {
        return context.Context switch
        {
            IOContext.UsageContext.MERGE => 64 * 1024,  // 64KB for merges
            IOContext.UsageContext.READ => 16 * 1024,   // 16KB for normal reads (reduces cache validation overhead)
            _ => 8 * 1024                                // 8KB default
        };
    }
}
