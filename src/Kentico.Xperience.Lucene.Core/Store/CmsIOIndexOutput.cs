using Lucene.Net.Store;
using Lucene.Net.Util;

using CmsPath = CMS.IO.Path;
using CmsFileStream = CMS.IO.FileStream;
using CmsFileMode = CMS.IO.FileMode;
using CmsFileAccess = CMS.IO.FileAccess;
using CmsFileShare = CMS.IO.FileShare;

namespace Kentico.Xperience.Lucene.Core.Store;

/// <summary>
/// FSIndexOutput implementation using CMS.IO
/// </summary>
internal class CmsIOIndexOutput : BufferedIndexOutput
{
    private const int CHUNK_SIZE = DEFAULT_BUFFER_SIZE;

    private readonly CmsIODirectory parent;
    internal readonly string Name;
    private readonly CmsFileStream file;
    private volatile bool isOpen;
    private readonly Crc32 crc = new();

    public CmsIOIndexOutput(CmsIODirectory parent, string name)
        : base(CHUNK_SIZE)
    {
        this.parent = parent;
        Name = name;
        file = CmsFileStream.New(
            CmsPath.Combine(parent.InternalDirectoryPath, name),
            CmsFileMode.Create,
            CmsFileAccess.Write,
            CmsFileShare.ReadWrite);
        isOpen = true;
    }

    public override void WriteByte(byte b)
    {
        if (!isOpen)
        {
            throw new ObjectDisposedException(GetType().FullName, "This CmsIOFSIndexOutput is disposed.");
        }

        crc.Update([b], 0, 1);
        file.WriteByte(b);
    }

    public override void WriteBytes(byte[] b, int offset, int length)
    {
        if (!isOpen)
        {
            throw new ObjectDisposedException(GetType().FullName, "This CmsIOFSIndexOutput is disposed.");
        }

        crc.Update(b, offset, length);
        file.Write(b, offset, length);
    }

    protected override void FlushBuffer(byte[] b, int offset, int size)
    {
        if (!isOpen)
        {
            throw new ObjectDisposedException(GetType().FullName, "This CmsIOFSIndexOutput is disposed.");
        }

        crc.Update(b, offset, size);
        file.Write(b, offset, size);
    }

    public override void Flush()
    {
        if (!isOpen)
        {
            throw new ObjectDisposedException(GetType().FullName, "This CmsIOFSIndexOutput is disposed.");
        }

        file.Flush();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            parent.OnIndexOutputClosed(this);
            if (isOpen)
            {
                Exception? priorE = null;
                try
                {
                    file.Flush();
                }
                finally
                {
                    isOpen = false;
                    IOUtils.DisposeWhileHandlingException(priorE, file);
                }
            }
        }
    }

    public override long Length => file.Length;

    public override long Checksum => crc.Value;

    public override long Position => file.Position;
}
