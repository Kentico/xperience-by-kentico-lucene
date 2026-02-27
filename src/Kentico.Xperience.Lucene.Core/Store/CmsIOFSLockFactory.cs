using Lucene.Net.Store;

using CmsDirectory = CMS.IO.Directory;

namespace Kentico.Xperience.Lucene.Core.Store;

/// <summary>
/// Base class for CMS.IO-based file system <see cref="LockFactory"/> implementations.
/// </summary>
public abstract class CmsIOFSLockFactory : LockFactory
{
    /// <summary>
    /// Directory for the lock files
    /// </summary>
    protected string? m_lockDir;

    /// <summary>
    /// Gets the lock directory. May be null if setLockDir() has not been called.
    /// </summary>
    public virtual string? LockDir => m_lockDir;

    /// <summary>
    /// Set the lock directory. This method can be called only once.
    /// </summary>
    /// <param name="lockDir">The lock directory</param>
    public virtual void SetLockDir(string lockDir)
    {
        if (m_lockDir != null)
        {
            throw new InvalidOperationException("You can set the lock directory for this factory only once.");
        }
        m_lockDir = lockDir;
    }

    /// <summary>
    /// Gets or sets the lock prefix. Default is null (none).
    /// </summary>
    public virtual string? LockPrefix { get; set; }

    /// <summary>
    /// Returns the lock prefix
    /// </summary>
    protected string? m_lockPrefix
    {
        get => LockPrefix;
        set => LockPrefix = value;
    }

    public override string ToString()
        => $"{GetType().Name}@{m_lockDir ?? "null"}";
}
