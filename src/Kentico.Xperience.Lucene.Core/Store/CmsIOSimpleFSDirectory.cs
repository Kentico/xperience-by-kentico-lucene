using Lucene.Net.Store;

using CmsPath = CMS.IO.Path;
using CmsFileStream = CMS.IO.FileStream;
using CmsFileMode = CMS.IO.FileMode;
using CmsFileAccess = CMS.IO.FileAccess;
using CmsFileShare = CMS.IO.FileShare;
using IOContext = Lucene.Net.Store.IOContext;

namespace Kentico.Xperience.Lucene.Core.Store;

/// <summary>
/// A straightforward implementation of <see cref="CmsIODirectory"/> using CMS.IO.FileStream.
/// </summary>
/// <remarks>
/// <para>
/// This implementation is ideal for use cases where efficient writing is required
/// without utilizing too much RAM. However, reading is less efficient than when using
/// <see cref="CmsIOMemoryMapDirectory"/>. This class has poor concurrent read performance
/// (multiple threads will bottleneck) as it synchronizes when multiple threads read
/// from the same file.
/// </para>
/// </remarks>
public class CmsIOSimpleFSDirectory : CmsIODirectory
{
    /// <summary>
    /// Create a new CmsIOSimpleFSDirectory for the named location
    /// </summary>
    /// <param name="path">The path of the directory</param>
    /// <param name="lockFactory">The lock factory to use, or null for the default</param>
    public CmsIOSimpleFSDirectory(string path, LockFactory? lockFactory = null)
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
        var raf = CmsFileStream.New(path, CmsFileMode.Open, CmsFileAccess.Read, CmsFileShare.ReadWrite);
        return new CmsIOSimpleFSIndexInput($"CmsIOSimpleFSIndexInput(path=\"{path}\")", raf, context);
    }

    public override IndexInputSlicer CreateSlicer(string name, IOContext context)
    {
        EnsureOpen();
        string path = CmsPath.Combine(Directory, name);
        var descriptor = CmsFileStream.New(path, CmsFileMode.Open, CmsFileAccess.Read, CmsFileShare.ReadWrite);
        return new IndexInputSlicerAnonymousClass(context, path, descriptor);
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
            return new CmsIOSimpleFSIndexInput(
                $"CmsIOSimpleFSIndexInput({sliceDescription} in path=\"{path}\" slice={offset}:{offset + length})",
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
    /// Reads bytes using CMS.IO.FileStream with synchronization
    /// </summary>
    protected internal class CmsIOSimpleFSIndexInput : BufferedIndexInput
    {
        private int disposed = 0;

        /// <summary>
        /// The file stream we will read from
        /// </summary>
        protected internal readonly CmsFileStream m_file;

        /// <summary>
        /// Is this instance a clone and hence does not own the file to close it
        /// </summary>
        public bool IsClone { get; set; }

        /// <summary>
        /// Start offset: non-zero in the slice case
        /// </summary>
        protected internal readonly long m_off;

        /// <summary>
        /// End offset (start+length)
        /// </summary>
        protected internal readonly long m_end;

        public CmsIOSimpleFSIndexInput(string resourceDesc, CmsFileStream file, IOContext context)
            : base(resourceDesc, context)
        {
            m_file = file;
            m_off = 0L;
            m_end = file.Length;
            IsClone = false;
        }

        public CmsIOSimpleFSIndexInput(string resourceDesc, CmsFileStream file, long off, long length, int bufferSize)
            : base(resourceDesc, bufferSize)
        {
            m_file = file;
            m_off = off;
            m_end = off + length;
            IsClone = true;
        }

        protected override void Dispose(bool disposing)
        {
            if (0 != Interlocked.CompareExchange(ref disposed, 1, 0))
            {
                return;
            }

            if (disposing && !IsClone)
            {
                m_file.Dispose();
            }
        }

        public override object Clone()
        {
            var clone = (CmsIOSimpleFSIndexInput)base.Clone();
            clone.IsClone = true;
            return clone;
        }

        public sealed override long Length => m_end - m_off;

        /// <summary>
        /// IndexInput methods
        /// </summary>
        protected override void ReadInternal(byte[] b, int offset, int len)
        {
            lock (m_file)
            {
                long position = m_off + Position;
                m_file.Seek(position, SeekOrigin.Begin);
                int total = 0;

                if (position + len > m_end)
                {
                    throw new EndOfStreamException("read past EOF: " + this);
                }

                try
                {
                    total = m_file.Read(b, offset, len);
                }
                catch (IOException ioe)
                {
                    throw new IOException(ioe.Message + ": " + this, ioe);
                }
            }
        }

        protected override void SeekInternal(long pos)
        {
        }

        public virtual bool IsFDValid => m_file != null;
    }
}
