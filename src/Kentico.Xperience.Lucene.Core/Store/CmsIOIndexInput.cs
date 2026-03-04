using Lucene.Net.Store;

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
internal class CmsIOIndexInput : BufferedIndexInput
{
    private const string RESOURCE_NAME = "CmsIOIndexInput";

    private readonly string path;
    private readonly long length;

    private CmsFileStream? stream;
    private bool isDisposed;
    private readonly bool isClone;


    /// <summary>
    /// Creates a new CmsIOIndexInput for reading the specified file.
    /// </summary>
    /// <param name="path">The full path to the file to read.</param>
    /// <param name="context">The IO context for buffer size hints.</param>
    public CmsIOIndexInput(string path, IOContext context)
        : base($"{RESOURCE_NAME}(path=\"{path}\")", DetermineBufferSize(context))
    {
        this.path = path;

        stream = CmsFileStream.New(path, CmsFileMode.Open, CmsFileAccess.Read, CmsFileShare.ReadWrite);
        length = stream.Length;
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
        isClone = true;
    }


    /// <summary>
    /// Gets the total length of the file in bytes.
    /// </summary>
    public override long Length => length;


    /// <summary>
    /// Reads bytes from the file into the buffer at the specified position.
    /// This is the core read method called by BufferedIndexInput.
    /// </summary>
    protected override void ReadInternal(byte[] b, int offset, int length)
    {
        if (isDisposed || stream == null)
        {
            throw new ObjectDisposedException(GetType().FullName);
        }

        lock (stream)
        {
            long position = Position;
            if (position != stream.Position)
            {
                stream.Seek(position, SeekOrigin.Begin);
            }

            int totalRead = 0;
            while (totalRead < length)
            {
                int bytesRead = stream.Read(b, offset + totalRead, length - totalRead);
                if (bytesRead == 0)
                {
                    throw new EndOfStreamException($"Read past EOF: {this}");
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
    /// The clone shares the same underlying stream but maintains its own position.
    /// </summary>
    public override object Clone()
    {
        return new CmsIOIndexInput(
            $"CmsIOIndexInput(path=\"{path}\") [clone]",
            path,
            stream!,
            length,
            BufferSize);
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

        if (disposing && !isClone)
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
        else
        {
            isDisposed = true;
        }
    }


    /// <summary>
    /// Determines the appropriate buffer size based on the IOContext.
    /// Larger buffers reduce the number of read operations, which is important
    /// for CMS.IO with Azure Blob Storage where each read may trigger cache validation.
    /// </summary>
    private static int DetermineBufferSize(IOContext context)
    {
#pragma warning disable IDE0072 // Missing cases are intentional here to provide specific buffer sizes for known contexts
        return context.Context switch
        {
            IOContext.UsageContext.MERGE => 64 * 1024,  // 64KB for merges
            IOContext.UsageContext.READ => 16 * 1024,   // 16KB for normal reads (reduces cache validation overhead)
            _ => 8 * 1024                                // 8KB default
        };
#pragma warning restore IDE0072
    }
}
