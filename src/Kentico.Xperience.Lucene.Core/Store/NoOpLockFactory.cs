using Lucene.Net.Store;

namespace Kentico.Xperience.Lucene.Core.Store;

/// <summary>
/// A no-op lock factory for use when external locking (e.g., FileLock) handles
/// write coordination. This is appropriate for Azure Blob Storage scenarios
/// where CMS.IO file-based locking doesn't work.
/// </summary>
public class NoOpLockFactory : LockFactory
{
    /// <summary>
    /// Singleton instance of the no-op lock factory.
    /// </summary>
    public static readonly NoOpLockFactory Instance = new();

    private NoOpLockFactory() { }

    /// <summary>
    /// Returns a no-op lock that always succeeds.
    /// </summary>
    public override Lock MakeLock(string lockName) => NoOpLock.Instance;

    /// <summary>
    /// No-op - there's nothing to clear.
    /// </summary>
    public override void ClearLock(string lockName) { }
}

/// <summary>
/// A lock that always succeeds immediately. Used with NoOpLockFactory.
/// </summary>
public class NoOpLock : Lock
{
    /// <summary>
    /// Singleton instance.
    /// </summary>
    public static readonly NoOpLock Instance = new();

    private NoOpLock() { }

    /// <summary>
    /// Always returns true - external locking handles coordination.
    /// </summary>
    public override bool Obtain() => true;

    /// <summary>
    /// Always returns false - we don't track lock state internally.
    /// </summary>
    public override bool IsLocked() => false;

    /// <summary>
    /// No-op disposal.
    /// </summary>
    protected override void Dispose(bool disposing) { }
}
