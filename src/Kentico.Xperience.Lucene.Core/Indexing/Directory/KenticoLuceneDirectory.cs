using Lucene.Net.Store;
using Lucene.Net.Util;

using System.Globalization;
using System.Runtime.CompilerServices;

using CMS.IO;

using Path = CMS.IO.Path;
using FileStream = CMS.IO.FileStream;
using DirectoryInfo = CMS.IO.DirectoryInfo;
using File = CMS.IO.File;
using FileInfo = CMS.IO.FileInfo;
using FileMode = CMS.IO.FileMode;
using FileAccess = CMS.IO.FileAccess;
using FileShare = CMS.IO.FileShare;

namespace Kentico.Xperience.Lucene.Core.Indexing;

public class KenticoLuceneDirectory : BaseDirectory
{
    public const int DEFAULT_READ_CHUNK_SIZE = 8192;

    protected readonly DirectoryInfo DirectoryInternal;
#pragma warning disable 612, 618
    private int chunkSize = DEFAULT_READ_CHUNK_SIZE;
#pragma warning restore 612, 618

    protected KenticoLuceneDirectory(DirectoryInfo dir)
        : this(dir, null)
    {
    }

    protected KenticoLuceneDirectory(DirectoryInfo path, LockFactory? lockFactory)
    {
        lockFactory ??= new KenticoFSLockFactory();
        DirectoryInternal = DirectoryInfo.New(path.GetCanonicalPath());

        if (File.Exists(path.FullName))
        {
            throw new DirectoryNotFoundException("file '" + path.FullName + "' exists but is not a directory");
        }

        SetLockFactory(lockFactory);
    }

    public override IndexInput OpenInput(string name, IOContext context)
    {
        EnsureOpen();
        var path = FileInfo.New(Path.Combine(Directory.FullName, name));
        var raf = FileStream.New(path.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        return new SimpleInput("SimpleFSIndexInput(path=\"" + path.FullName + "\")", raf, context);
    }

    public static KenticoLuceneDirectory Open(DirectoryInfo path) => Open(path, null);

    public static KenticoLuceneDirectory Open(string path)
        => Open(DirectoryInfo.New(path), null);

    public static KenticoLuceneDirectory Open(DirectoryInfo path, LockFactory? lockFactory) => new(path, lockFactory);

    public static KenticoLuceneDirectory Open(string path, LockFactory lockFactory)
        => Open(DirectoryInfo.New(path), lockFactory);

    public override void SetLockFactory(LockFactory lockFactory)
    {
        base.SetLockFactory(lockFactory);

        if (lockFactory is KenticoLockFactory lf)
        {
            var dir = lf.LockDir;
            if (dir is null)
            {
                lf.SetLockDir(DirectoryInternal);
                lf.LockPrefix = null;
            }
            else if (dir.GetCanonicalPath().Equals(DirectoryInternal.GetCanonicalPath(), StringComparison.Ordinal))
            {
                lf.LockPrefix = null;
            }
        }
    }

    public static string[] ListAll(DirectoryInfo dir)
    {
        if (!System.IO.Directory.Exists(dir.FullName))
        {
            throw new DirectoryNotFoundException("directory '" + dir + "' does not exist");
        }
        else if (File.Exists(dir.FullName))
        {
            throw new DirectoryNotFoundException("file '" + dir + "' exists but is not a directory");
        }

        var files = dir.EnumerateFiles().ToArray();
        string[] result = new string[files.Length];

        for (int i = 0; i < files.Length; i++)
        {
            result[i] = files[i].Name;
        }

        return result;
    }

    public override string[] ListAll()
    {
        EnsureOpen();
        return ListAll(DirectoryInternal);
    }

    [Obsolete("this method will be removed in 5.0")]
    public override bool FileExists(string name)
    {
        EnsureOpen();
        return File.Exists(Path.Combine(DirectoryInternal.FullName, name));
    }

    public override long FileLength(string name)
    {
        EnsureOpen();
        var file = FileInfo.New(Path.Combine(DirectoryInternal.FullName, name));
        long len = file.Length;
        if (len == 0 && !file.Exists)
        {
            throw new FileNotFoundException(name);
        }
        else
        {
            return len;
        }
    }

    public override void DeleteFile(string name)
    {
        EnsureOpen();
        var file = FileInfo.New(Path.Combine(DirectoryInternal.FullName, name));
        if (!File.Exists(file.FullName))
        {
            throw new FileNotFoundException("Cannot delete " + file + " because it doesn't exist.");
        }
        try
        {
            file.Delete();
            if (File.Exists(file.FullName))
            {
                throw new IOException("Cannot delete " + file);
            }
        }
        catch (Exception e)
        {
            throw new IOException("Cannot delete " + file, e);
        }
    }

    public override IndexOutput CreateOutput(string name, IOContext context)
    {
        EnsureOpen();

        EnsureCanWrite(name);
        return new KenticoLuceneIndexOutput(this, name);
    }

    protected virtual void EnsureCanWrite(string name)
    {
        if (!DirectoryInternal.Exists)
        {
            try
            {
                DirectoryHelper.EnsureDiskPath(DirectoryInternal.GetCanonicalPath(), "");
            }
            catch
            {
                throw new IOException("Cannot create directory: " + DirectoryInternal);
            }
        }

        var file = FileInfo.New(Path.Combine(DirectoryInternal.FullName, name));
        if (file.Exists) // delete existing, if any
        {
            try
            {
                file.Delete();
            }
            catch
            {
                throw new IOException("Cannot overwrite: " + file);
            }
        }
    }

    protected virtual void OnIndexOutputClosed(KenticoLuceneIndexOutput io)
    {
        // LUCENENET specific: No such thing as "stale files" in .NET, since Flush(true) writes everything to disk before
        // our FileStream is disposed.
        //m_staleFiles.Add(io.name);
    }

    public override void Sync(ICollection<string> names) => EnsureOpen();

    public override string GetLockID()
    {
        EnsureOpen();
        string dirName = DirectoryInternal.GetCanonicalPath();
        int digest = 0;
        for (int charIDX = 0; charIDX
            < dirName.Length; charIDX++)
        {
            char ch = dirName[charIDX];
            digest = 31 * digest + ch;
        }
        return "lucene-" + digest.ToString("x", CultureInfo.InvariantCulture);
    }

    protected override void Dispose(bool disposing) => IsOpen = false;

    public virtual DirectoryInfo Directory
    {
        get
        {
            EnsureOpen();
            return DirectoryInternal;
        }
    }

    public override string ToString() => GetType().Name + "@" + DirectoryInternal + " lockFactory=" + LockFactory;

    [Obsolete("this is no longer used since Lucene 4.5.")]
    public int ReadChunkSize
    {
        get => chunkSize;
        set
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(ReadChunkSize), "chunkSize must be positive"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }

            chunkSize = value;
        }
    }

    protected class KenticoLuceneIndexOutput : BufferedIndexOutput
    {
        private const int CHUNK_SIZE = DEFAULT_BUFFER_SIZE;

        private readonly KenticoLuceneDirectory parent;
        internal readonly string Name;
#pragma warning disable CA2213 // Disposable fields should be disposed
        private readonly FileStream file;
#pragma warning restore CA2213 // Disposable fields should be disposed
        private volatile bool isOpen; // remember if the file is open, so that we don't try to close it more than once
        private readonly CheckSum crc = new();

        public KenticoLuceneIndexOutput(KenticoLuceneDirectory parent, string name)
            : base(CHUNK_SIZE)
        {
            this.parent = parent;
            Name = name;
            file = FileStream.New(
                path: Path.Combine(parent.DirectoryInternal.FullName, name),
                mode: FileMode.OpenOrCreate,
                access: FileAccess.Write,
                share: FileShare.ReadWrite,
                bufferSize: CHUNK_SIZE);
            isOpen = true;
        }

        /// <inheritdoc/>
        public override void WriteByte(byte b)
        {
            if (!isOpen)
            {
                throw new InvalidOperationException("This directory is disposed." + GetType().FullName);
            }

            crc.Update(b);
            file.WriteByte(b);
        }

        public override void WriteBytes(byte[] b, int offset, int length)
        {
            if (!isOpen)
            {
                throw new InvalidOperationException("This directory is disposed." + GetType().FullName);
            }

            crc.Update(b, offset, length);
            file.Write(b, offset, length);
        }

        protected override void FlushBuffer(byte[] b, int offset, int size)
        {
            if (!isOpen)
            {
                throw new InvalidOperationException("This directory is disposed." + GetType().FullName);
            }

            crc.Update(b, offset, size);
            file.Write(b, offset, size);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override void Flush()
        {
            if (!isOpen)
            {
                throw new InvalidOperationException("This directory is disposed." + GetType().FullName);
            }

            file.Flush();
        }

        public bool IsIOException(Exception e)
        {
            if (e is null)
            {
                return false;
            }

            return e is IOException or
                ObjectDisposedException or
                UnauthorizedAccessException;
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
                    catch (Exception ioe) when (IsIOException(ioe))
                    {
                        priorE = ioe;
                    }
                    finally
                    {
                        isOpen = false;
                        IOUtils.DisposeWhileHandlingException(priorE, file);
                    }
                }
            }
        }

        [Obsolete("(4.1) this method will be removed in Lucene 5.0")]
        public override void Seek(long pos)
        {
            if (!isOpen)
            {
                throw new InvalidOperationException("This directory is disposed." + GetType().FullName);
            }

            file.Seek(pos, SeekOrigin.Begin);
        }

        public override long Length => file.Length;

        public override long Checksum => crc.Value;

        public override long Position => file.Position;
    }
}
