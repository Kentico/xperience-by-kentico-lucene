using J2N.IO;
using Lucene.Net.Store;

using CmsPath = CMS.IO.Path;
using CmsFileStream = CMS.IO.FileStream;
using CmsFileMode = CMS.IO.FileMode;
using CmsFileAccess = CMS.IO.FileAccess;
using CmsFileShare = CMS.IO.FileShare;
using IOContext = Lucene.Net.Store.IOContext;

namespace Kentico.Xperience.Lucene.Core.Store;

/// <summary>
/// A <see cref="CmsIODirectory"/> implementation that uses CMS.IO.FileStream's
/// positional reading, which allows multiple threads to read from the same file
/// without synchronizing.
/// </summary>
/// <remarks>
/// <para>
/// This class uses positional seeking during reads, which may be slightly less
/// efficient than <see cref="CmsIOSimpleFSDirectory"/> during single-threaded reading.
/// However, it provides better concurrent read performance.
/// </para>
/// </remarks>
public class CmsIONIOFSDirectory : CmsIODirectory
{
    /// <summary>
    /// Create a new CmsIONIOFSDirectory for the named location
    /// </summary>
    /// <param name="path">The path of the directory</param>
    /// <param name="lockFactory">The lock factory to use, or null for the default</param>
    public CmsIONIOFSDirectory(string path, LockFactory? lockFactory = null)
        : base(path, lockFactory)
    {
    }

    /// <summary>
    /// Creates an IndexInput for the file with the given name
    /// </summary>
    public override IndexInput OpenInput(string name, IOContext context)
    {
        EnsureOpen();
        string path = CmsPath.Combine(Directory, name);
        var fc = CmsFileStream.New(path, CmsFileMode.Open, CmsFileAccess.Read, CmsFileShare.ReadWrite);
        return new CmsIONIOFSIndexInput($"CmsIONIOFSIndexInput(path=\"{path}\")", fc, context);
    }

    public override IndexInputSlicer CreateSlicer(string name, IOContext context)
    {
        EnsureOpen();
        string path = CmsPath.Combine(Directory, name);
        var fc = CmsFileStream.New(path, CmsFileMode.Open, CmsFileAccess.Read, CmsFileShare.ReadWrite);
        return new IndexInputSlicerAnonymousClass(context, path, fc);
    }

    private sealed class IndexInputSlicerAnonymousClass : IndexInputSlicer
    {
        private readonly IOContext context;
        private readonly string path;
        private readonly CmsFileStream descriptor;
        private int disposed = 0;

        public IndexInputSlicerAnonymousClass(IOContext context, string path, CmsFileStream descriptor)
        {
            this.context = context;
            this.path = path;
            this.descriptor = descriptor;
        }

        protected override void Dispose(bool disposing)
        {
            if (0 != Interlocked.CompareExchange(ref disposed, 1, 0))
            {
                return;
            }

            if (disposing)
            {
                descriptor.Dispose();
            }
        }

        public override IndexInput OpenSlice(string sliceDescription, long offset, long length)
        {
            return new CmsIONIOFSIndexInput(
                $"CmsIONIOFSIndexInput({sliceDescription} in path=\"{path}\" slice={offset}:{offset + length})",
                descriptor,
                offset,
                length,
                BufferedIndexInput.GetBufferSize(context));
        }

        [Obsolete("Only for reading CFS files from 3.x indexes.")]
        public override IndexInput OpenFullSlice()
        {
            return OpenSlice("full-slice", 0, descriptor.Length);
        }
    }

    /// <summary>
    /// Reads bytes using positional reading with CMS.IO.FileStream
    /// </summary>
    protected class CmsIONIOFSIndexInput : BufferedIndexInput
    {
        /// <summary>
        /// The maximum chunk size for reads of 16384 bytes
        /// </summary>
        private const int CHUNK_SIZE = 16384;

        /// <summary>
        /// The file stream we will read from
        /// </summary>
        protected internal readonly CmsFileStream m_channel;

        /// <summary>
        /// Is this instance a clone and hence does not own the file to close it
        /// </summary>
        internal bool isClone = false;

        /// <summary>
        /// Start offset: non-zero in the slice case
        /// </summary>
        protected internal readonly long m_off;

        /// <summary>
        /// End offset (start+length)
        /// </summary>
        protected internal readonly long m_end;

        private ByteBuffer? byteBuf;

        private int disposed = 0;

        public CmsIONIOFSIndexInput(string resourceDesc, CmsFileStream fc, IOContext context)
            : base(resourceDesc, context)
        {
            m_channel = fc;
            m_off = 0L;
            m_end = fc.Length;
        }

        public CmsIONIOFSIndexInput(string resourceDesc, CmsFileStream fc, long off, long length, int bufferSize)
            : base(resourceDesc, bufferSize)
        {
            m_channel = fc;
            m_off = off;
            m_end = off + length;
            isClone = true;
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
            var clone = (CmsIONIOFSIndexInput)base.Clone();
            clone.isClone = true;
            return clone;
        }

        public sealed override long Length => m_end - m_off;

        protected override void NewBuffer(byte[] newBuffer)
        {
            base.NewBuffer(newBuffer);
            byteBuf = ByteBuffer.Wrap(newBuffer);
        }

        protected override void ReadInternal(byte[] b, int offset, int length)
        {
            ByteBuffer bb;

            // Determine the ByteBuffer we should use
            if (b == m_buffer && 0 == offset)
            {
                // Use our own pre-wrapped byteBuf:
                byteBuf ??= ByteBuffer.Wrap(b);
                byteBuf.Clear();
                byteBuf.Limit = length;
                bb = byteBuf;
            }
            else
            {
                bb = ByteBuffer.Wrap(b, offset, length);
            }

            int readOffset = bb.Position;
            int readLength = bb.Limit - readOffset;
            long pos = Position + m_off;

            if (pos + length > m_end)
            {
                throw new EndOfStreamException("read past EOF: " + this);
            }

            try
            {
                while (readLength > 0)
                {
                    int toRead = Math.Min(CHUNK_SIZE, readLength);
                    bb.Limit = readOffset + toRead;

                    // Read using positional access (simulated via Seek + Read for CMS.IO)
                    m_channel.Seek(pos, SeekOrigin.Begin);
                    int i = m_channel.Read(bb.Array, bb.Position, toRead);
                    bb.Position += i;

                    if (i <= 0)
                    {
                        throw new EndOfStreamException($"read past EOF: {this} off: {offset} len: {length} pos: {pos} chunkLen: {readLength} end: {m_end}");
                    }
                    pos += i;
                    readOffset += i;
                    readLength -= i;
                }
            }
            catch (IOException ioe)
            {
                throw new IOException(ioe.ToString() + ": " + this, ioe);
            }
        }

        protected override void SeekInternal(long pos)
        {
        }
    }
}
