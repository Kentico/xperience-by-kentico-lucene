using Lucene.Net.Store;

using Directory = Lucene.Net.Store.Directory;

namespace Kentico.Xperience.Lucene.Core.Indexing;
public abstract class BaseDirectory : Directory
{
    private volatile bool isOpen = true;

    protected internal virtual bool IsOpen
    {
        get => isOpen;
        set => isOpen = value;
    }

    protected internal LockFactory? LockFactoryInternal;

    protected internal BaseDirectory()
        : base()
    {
    }

    public override Lock MakeLock(string name) => LockFactoryInternal!.MakeLock(name);

    public override void ClearLock(string name) => LockFactoryInternal?.ClearLock(name);

    public override void SetLockFactory(LockFactory lockFactory)
    {
        LockFactoryInternal = lockFactory;
        lockFactory.LockPrefix = GetLockID();
    }

    public override LockFactory LockFactory => LockFactoryInternal!;

    protected override void EnsureOpen()
    {
        if (!IsOpen)
        {
            throw new InvalidOperationException("This directory is disposed." + GetType().FullName);
        }
    }
}
