using Lucene.Net.Store;

using DirectoryInfo = CMS.IO.DirectoryInfo;

namespace Kentico.Xperience.Lucene.Core.Indexing;
public abstract class KenticoLockFactory : LockFactory
{
    protected internal DirectoryInfo? LockDirInternal = null;

    protected internal void SetLockDir(DirectoryInfo lockDir)
    {
        if (LockDirInternal != null)
        {
            throw new InvalidOperationException("You can set the lock directory for this factory only once.");
        }
        LockDirInternal = lockDir;
    }

    public DirectoryInfo LockDir => LockDirInternal!;

    public override string ToString() => GetType().Name + "@" + LockDirInternal;
}
