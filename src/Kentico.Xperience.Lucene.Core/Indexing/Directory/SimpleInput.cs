using Lucene.Net.Store;

using FileStream = CMS.IO.FileStream;
using File = CMS.IO.File;

namespace Kentico.Xperience.Lucene.Core.Indexing;
internal class SimpleInput : BufferedIndexInput
{
    private int disposed = 0;

    protected internal readonly FileStream File;

    public bool IsClone { get; set; }

    protected internal readonly long Off;

    protected internal readonly long End;

    public SimpleInput(string resourceDesc, FileStream file, IOContext context)
        : base(resourceDesc, context)
    {
        File = file;
        Off = 0L;
        End = file.Length;
        IsClone = false;
    }

    public SimpleInput(string resourceDesc, FileStream file, long off, long length, int bufferSize)
        : base(resourceDesc, bufferSize)
    {
        IsClone = true;
        File = file;
        Off = off;
        End = off + length;
    }

    protected override void Dispose(bool disposing)
    {
        if (0 != Interlocked.CompareExchange(ref disposed, 1, 0))
        {
            return;
        }

        if (disposing && !IsClone)
        {
            File.Dispose();
        }
    }

    public override object Clone()
    {
        var clone = (SimpleInput)base.Clone();
        clone.IsClone = true;
        return clone;
    }

    public override sealed long Length => End - Off;

    protected override void ReadInternal(byte[] b, int offset, int len)
    {
        UninterruptableMonitor.Enter(File);
        try
        {
            long position = Off + Position;
            File.Seek(position, SeekOrigin.Begin);
            int total = 0;

            if (position + len > End)
            {
                throw new InvalidOperationException("read past EOF: " + this);
            }

            try
            {
                total = File.Read(b, offset, len);
            }
            catch (Exception ioe) when (ioe is IOException)
            {
                throw new IOException(ioe.Message + ": " + this, ioe);
            }
        }
        finally
        {
            UninterruptableMonitor.Exit(File);
        }
    }

    protected override void SeekInternal(long position)
    {
    }

    public virtual bool IsFDValid => File != null;
}
