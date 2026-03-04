using Lucene.Net.Store;

using CmsFileStream = CMS.IO.FileStream;
using CmsFileMode = CMS.IO.FileMode;
using CmsFileAccess = CMS.IO.FileAccess;
using CmsFileShare = CMS.IO.FileShare;

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
            return crc.Value;
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
            crc.Update(b, offset, len);
            bytesWritten += len;
        }
    }


    /// <summary>
    /// Flushes all buffered data to the underlying stream and then to storage.
    /// </summary>
    public override void Flush()
    {
        base.Flush();
        stream.Flush();
    }


    /// <summary>
    /// Disposes of the underlying stream resources.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Ensure all data is flushed before closing
            try
            {
                Flush();
            }
            finally
            {
                stream?.Dispose();
            }
        }
    }
}


/// <summary>
/// Simple CRC32 implementation for checksum calculation.
/// </summary>
internal class Crc32
{
    private static readonly uint[] table = CreateTable();
    private uint crc = 0xFFFFFFFF;

    public long Value => crc ^ 0xFFFFFFFF;


    public void Update(byte[] buffer, int offset, int length)
    {
        for (int i = offset; i < offset + length; i++)
        {
            crc = (crc >> 8) ^ table[(crc ^ buffer[i]) & 0xFF];
        }
    }


    public void Reset()
    {
        crc = 0xFFFFFFFF;
    }


    private static uint[] CreateTable()
    {
        var newTable = new uint[256];
        const uint polynomial = 0xEDB88320;

        for (uint i = 0; i < 256; i++)
        {
            uint entry = i;
            for (int j = 0; j < 8; j++)
            {
                if ((entry & 1) == 1)
                {
                    entry = (entry >> 1) ^ polynomial;
                }
                else
                {
                    entry >>= 1;
                }
            }
            newTable[i] = entry;
        }

        return newTable;
    }
}
