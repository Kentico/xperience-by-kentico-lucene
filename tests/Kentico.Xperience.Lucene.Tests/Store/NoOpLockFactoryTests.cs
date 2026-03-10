using Kentico.Xperience.Lucene.Core.Store;

namespace Kentico.Xperience.Lucene.Tests.Store;

[TestFixture]
public class NoOpLockFactoryTests
{
    [Test]
    public void Instance_ReturnsSameInstance()
    {
        var instance1 = NoOpLockFactory.Instance;
        var instance2 = NoOpLockFactory.Instance;

        Assert.That(instance1, Is.SameAs(instance2));
    }


    [Test]
    public void MakeLock_ReturnsSameNoOpLockInstance()
    {
        var result = NoOpLockFactory.Instance.MakeLock("any.lock");

        Assert.That(result, Is.SameAs(NoOpLock.Instance));
    }


    [Test]
    public void MakeLock_WithDifferentNames_ReturnsSameInstance()
    {
        var lock1 = NoOpLockFactory.Instance.MakeLock("first.lock");
        var lock2 = NoOpLockFactory.Instance.MakeLock("second.lock");

        Assert.That(lock1, Is.SameAs(lock2));
    }


    [Test]
    public void ClearLock_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => NoOpLockFactory.Instance.ClearLock("test.lock"));
    }


    [Test]
    public void MakeLock_ReturnsInstanceOfNoOpLock()
    {
        var result = NoOpLockFactory.Instance.MakeLock("test.lock");

        Assert.That(result, Is.InstanceOf<NoOpLock>());
    }
}


[TestFixture]
public class NoOpLockTests
{
    [Test]
    public void Instance_ReturnsSameInstance()
    {
        var instance1 = NoOpLock.Instance;
        var instance2 = NoOpLock.Instance;

        Assert.That(instance1, Is.SameAs(instance2));
    }


    [Test]
    public void Obtain_ReturnsTrue()
    {
        bool result = NoOpLock.Instance.Obtain();

        Assert.That(result, Is.True);
    }


    [Test]
    public void IsLocked_ReturnsFalse()
    {
        bool result = NoOpLock.Instance.IsLocked();

        Assert.That(result, Is.False);
    }


    [Test]
    public void Obtain_CalledMultipleTimes_AlwaysReturnsTrue()
    {
        var noOpLock = NoOpLock.Instance;

        for (int i = 0; i < 10; i++)
        {
            Assert.That(noOpLock.Obtain(), Is.True, $"Obtain() should return true on call {i + 1}");
        }
    }


    [Test]
    public void IsLocked_CalledMultipleTimes_AlwaysReturnsFalse()
    {
        var noOpLock = NoOpLock.Instance;

        for (int i = 0; i < 10; i++)
        {
            Assert.That(noOpLock.IsLocked(), Is.False, $"IsLocked() should return false on call {i + 1}");
        }
    }


    [Test]
    public void Dispose_DoesNotThrow()
    {
        // Lock implements IDisposable via Lucene.Net.Store.Lock base class
        Assert.DoesNotThrow(() => NoOpLock.Instance.Dispose());
    }
}
