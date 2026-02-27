using System.IO;

using Lucene.Net.Store;
using Lucene.Net.Util;

using CmsFileAccess = CMS.IO.FileAccess;
using CmsFileMode = CMS.IO.FileMode;
using CmsFileShare = CMS.IO.FileShare;
using CmsFileStream = CMS.IO.FileStream;
using CmsPath = CMS.IO.Path;
using IOContext = Lucene.Net.Store.IOContext;

namespace Kentico.Xperience.Lucene.Core.Store;

/// <summary>
/// A <see cref="CmsIODirectory"/> implementation of Lucene.Net MMapDirectorythat attempts to use memory-mapped IO
/// when supported by the underlying CMS.IO storage provider.
/// </summary>
/// <remarks>
/// <para>
/// Note: CMS.IO does not natively support memory-mapped files in the same way as System.IO.
/// This implementation falls back to buffered positional reading similar to <see cref="CmsIONIOFSDirectory"/>.
/// </para>
/// <para>
/// For Azure Blob Storage and other remote storage providers, memory mapping is not applicable.
/// This class provides the same interface as MMapDirectory but uses CMS.IO streaming instead.
/// </para>
/// </remarks>
public class CmsIOMemoryMapDirectory : CmsIODirectory
{
    /// <summary>
    /// Default max chunk size
    /// </summary>
    public static readonly int DEFAULT_MAX_BUFF = Constants.RUNTIME_IS_64BIT ? (1 << 30) : (1 << 28);

    private readonly int chunkSizePower;

    /// <summary>
    /// Create a new CmsIOMMapDirectory for the named location
    /// </summary>
    /// <param name="path">The path of the directory</param>
    /// <param name="lockFactory">The lock factory to use, or null for the default</param>
    public CmsIOMemoryMapDirectory(string path, LockFactory? lockFactory = null)
        : this(path, lockFactory, DEFAULT_MAX_BUFF)
    {
    }

    /// <summary>
    /// Create a new CmsIOMMapDirectory with a specified max chunk size
    /// </summary>
    /// <param name="path">The path of the directory</param>
    /// <param name="lockFactory">The lock factory to use, or null for the default</param>
    /// <param name="maxChunkSize">Maximum chunk size used for reading</param>
    public CmsIOMemoryMapDirectory(string path, LockFactory? lockFactory, int maxChunkSize)
        : base(path, lockFactory)
    {
        if (maxChunkSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxChunkSize), "Maximum chunk size for mmap must be > 0");
        }
        chunkSizePower = 31 - int.LeadingZeroCount(maxChunkSize);
    }

    /// <summary>
    /// Returns the current mmap chunk size
    /// </summary>
    public int MaxChunkSize => 1 << chunkSizePower;

    /// <summary>
    /// Creates an IndexInput for the file with the given name
    /// </summary>
    public override IndexInput OpenInput(string name, IOContext context)
    {
        EnsureOpen();
        string path = CmsPath.Combine(Directory, name);
        //var fc = CmsFileStream.New(path, CmsFileMode.Open, CmsFileAccess.Read, CmsFileShare.ReadWrite);
        return new CmsIOIndexInput(path, context);
    }

    //public override IndexInputSlicer CreateSlicer(string name, IOContext context)
    //{
    //    EnsureOpen();
    //    string path = CmsPath.Combine(Directory, name);
    //    var fc = CmsFileStream.New(path, CmsFileMode.Open, CmsFileAccess.Read, CmsFileShare.ReadWrite);
    //    return new IndexInputSlicerAnonymousClass(this, context, path, fc);
    //}

    //private sealed class IndexInputSlicerAnonymousClass : IndexInputSlicer
    //{
    //    private readonly CmsIOMemoryMapDirectory outerInstance;
    //    private readonly IOContext context;
    //    private readonly string path;
    //    private readonly CmsFileStream descriptor;
    //    private int disposed = 0;

    //    public IndexInputSlicerAnonymousClass(CmsIOMemoryMapDirectory outerInstance, IOContext context, string path, CmsFileStream descriptor)
    //    {
    //        this.outerInstance = outerInstance;
    //        this.context = context;
    //        this.path = path;
    //        this.descriptor = descriptor;
    //    }

    //    public override IndexInput OpenSlice(string sliceDescription, long offset, long length)
    //    {
    //        //outerInstance.EnsureOpen();
    //        //return new CmsIOMemoryMapIndexInput(
    //        //    $"CmsIOMMapIndexInput({sliceDescription} in path=\"{path}\" slice={offset}:{offset + length})",
    //        //    descriptor,
    //        //    offset,
    //        //    length,
    //        //    BufferedIndexInput.GetBufferSize(context),
    //        //    outerInstance.chunkSizePower);
    //    }

    //    [Obsolete("Only for reading CFS files from 3.x indexes.")]
    //    public override IndexInput OpenFullSlice()
    //    {
    //        outerInstance.EnsureOpen();

    //        return OpenSlice("full-slice", 0, descriptor.Length);
    //    }

    //    protected override void Dispose(bool disposing)
    //    {
    //        if (0 != Interlocked.CompareExchange(ref disposed, 1, 0))
    //        {
    //            return;
    //        }

    //        if (disposing)
    //        {
    //            descriptor.Dispose();
    //        }
    //    }
    //}


    /// <summary>
    /// IndexInput implementation that simulates memory-mapped behavior using CMS.IO streams
    /// </summary>
    protected class CmsIOMemoryMapIndexInput : BufferedIndexInput
    {
        private readonly CmsFileStream m_channel;
        internal bool isClone = false;
        protected internal readonly long m_off;
        protected internal readonly long m_end;
        private readonly int chunkSizePower;
        private int disposed = 0;

        public CmsIOMemoryMapIndexInput(string resourceDesc, CmsFileStream fc, IOContext context, int chunkSizePower)
            : base(resourceDesc, context)
        {
            m_channel = fc;
            m_off = 0L;
            m_end = fc.Length;
            this.chunkSizePower = chunkSizePower;
        }

        public CmsIOMemoryMapIndexInput(string resourceDesc, CmsFileStream fc, long off, long length, int bufferSize, int chunkSizePower)
            : base(resourceDesc, bufferSize)
        {
            m_channel = fc;
            m_off = off;
            m_end = off + length;
            isClone = true;
            this.chunkSizePower = chunkSizePower;
        }

        protected override void Dispose(bool disposing)
        {
            if (0 != Interlocked.CompareExchange(ref disposed, 1, 0))
            {
                return;
            }

            if (disposing && !isClone)
            {
                m_channel.Dispose();
            }
        }

        public override object Clone()
        {
            var clone = (CmsIOMemoryMapIndexInput)base.Clone();
            clone.isClone = true;
            return clone;
        }

        public sealed override long Length => m_end - m_off;

        protected override void ReadInternal(byte[] b, int offset, int length)
        {
            lock (m_channel)
            {
                long position = m_off + Position;
                if (position + length > m_end)
                {
                    throw new IOException("read past EOF: " + this);
                }

                try
                {
                    m_channel.Seek(position, SeekOrigin.Begin);
                    int total = m_channel.Read(b, offset, length);

                    if (total != length)
                    {
                        throw new IOException($"read past EOF: {this} off: {offset} len: {length} total: {total}");
                    }
                }
                catch (Exception ioe)
                {
                    throw new IOException(ioe.ToString() + ": " + this, ioe);
                }
            }
        }

        protected override void SeekInternal(long pos)
        {
        }

        //internal IndexInput Slice(string sliceDescription, long offset, long length)
        //{
        //    if (offset < 0 || length < 0 || offset + length > Length)
        //    {
        //        throw new ArgumentException($"slice() {sliceDescription} out of bounds: {this}");
        //    }

        //    return new CmsIOMemoryMapIndexInput(
        //        sliceDescription,
        //        m_channel,
        //        m_off + offset,
        //        length,
        //        BufferSize,
        //        chunkSizePower);
        //}
    }
}
