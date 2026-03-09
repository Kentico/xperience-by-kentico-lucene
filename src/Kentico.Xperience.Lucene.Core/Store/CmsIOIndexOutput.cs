using System.IO.Hashing;

using Lucene.Net.Store;

using CmsFileAccess = CMS.IO.FileAccess;
using CmsFileMode = CMS.IO.FileMode;
using CmsFileShare = CMS.IO.FileShare;
using CmsFileStream = CMS.IO.FileStream;

namespace Kentico.Xperience.Lucene.Core.Store;

/// <summary>
/// A Lucene IndexOutput implementation that writes to CMS.IO FileStream.
/// This allows Lucene to write index files through the CMS.IO abstraction layer,
/// enabling storage on Azure Blob Storage or other custom storage providers.
/// </summary>
internal class CmsIOIndexOutput : BufferedIndexOutput
{
    private readonly CmsFileStream stream;
    private readonly Crc32 crc = new();
    private long bytesWritten;
    private bool disposed;


    /// <summary>
    /// Creates a new CmsIOIndexOutput for writing to the specified file.
    /// </summary>
    /// <param name="path">The full path to the file to write.</param>
    public CmsIOIndexOutput(string path)
        : base()
    {
        stream = CmsFileStream.New(path, CmsFileMode.Create, CmsFileAccess.Write, CmsFileShare.None);
        bytesWritten = 0;
    }


    /// <summary>
    /// Creates a new CmsIOIndexOutput with a specified buffer size.
    /// </summary>
    /// <param name="path">The full path to the file to write.</param>
    /// <param name="bufferSize">The buffer size to use.</param>
    public CmsIOIndexOutput(string path, int bufferSize)
        : base(bufferSize)
    {
        stream = CmsFileStream.New(path, CmsFileMode.Create, CmsFileAccess.Write, CmsFileShare.None);
        bytesWritten = 0;
    }


    /// <summary>
    /// Gets the current length of data written.
    /// </summary>
    public override long Length => bytesWritten;


    /// <summary>
    /// Gets the checksum of all data written so far.
    /// </summary>
    public override long Checksum
    {
        get
        {
            Flush();
            return crc.GetCurrentHashAsUInt32();
        }
    }


    /// <summary>
    /// Writes bytes from the buffer to the underlying stream.
    /// This is the core write method called by BufferedIndexOutput.
    /// </summary>
    protected override void FlushBuffer(byte[] b, int offset, int len)
    {
        if (len > 0)
        {
            stream.Write(b, offset, len);
            crc.Append(b.AsSpan(offset, len));
            bytesWritten += len;
        }
    }


    /// <summary>
    /// Flushes all buffered data to the underlying stream and then to storage.
    /// </summary>
    public override void Flush()
    {
        base.Flush();
        if (!disposed)
        {
            stream.Flush();
        }
    }


    /// <summary>
    /// Disposes of the underlying stream resources.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing && !disposed)
        {
            disposed = true;
            try
            {
                base.Dispose(disposing);
            }
            finally
            {
                stream?.Dispose();
            }
        }
    }
}
