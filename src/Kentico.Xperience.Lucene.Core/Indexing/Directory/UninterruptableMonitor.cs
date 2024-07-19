using ThreadInterruptedException = System.Threading.ThreadInterruptedException;

namespace Kentico.Xperience.Lucene.Core.Indexing;
internal static class UninterruptableMonitor
{
    public static void Enter(object obj, ref bool lockTaken)
    {
        try
        {
            Monitor.Enter(obj, ref lockTaken);
        }
        catch (Exception ie) when (ie is ThreadInterruptedException)
        {
            RetryEnter(obj, ref lockTaken);

            Thread.CurrentThread.Interrupt();
        }
    }

    private static void RetryEnter(object obj, ref bool lockTaken)
    {
        try
        {
            Monitor.Enter(obj, ref lockTaken);
        }
        catch (Exception ie) when (ie is ThreadInterruptedException)
        {
            RetryEnter(obj, ref lockTaken);
        }
    }

    public static void Enter(object obj)
    {
        try
        {
            Monitor.Enter(obj);
        }
        catch (Exception ie) when (ie is ThreadInterruptedException)
        {
            RetryEnter(obj);

            Thread.CurrentThread.Interrupt();
        }
    }

    private static void RetryEnter(object obj)
    {
        try
        {
            Monitor.Enter(obj);
        }
        catch (Exception ie) when (ie is ThreadInterruptedException)
        {
            RetryEnter(obj);
        }
    }

    public static void Exit(object obj) => Monitor.Exit(obj);

    public static bool IsEntered(object obj) => Monitor.IsEntered(obj);

    public static bool TryEnter(object obj) => Monitor.TryEnter(obj);

    public static void TryEnter(object obj, ref bool lockTaken) => Monitor.TryEnter(obj, ref lockTaken);

    public static bool TryEnter(object obj, int millisecondsTimeout) => Monitor.TryEnter(obj, millisecondsTimeout);

    public static bool TryEnter(object obj, TimeSpan timeout) => Monitor.TryEnter(obj, timeout);

    public static void TryEnter(object obj, int millisecondsTimeout, ref bool lockTaken) => Monitor.TryEnter(obj, millisecondsTimeout, ref lockTaken);

    public static void TryEnter(object obj, TimeSpan timeout, ref bool lockTaken) => Monitor.TryEnter(obj, timeout, ref lockTaken);

    public static void Pulse(object obj) => Monitor.Pulse(obj);

    public static void PulseAll(object obj) => Monitor.PulseAll(obj);

    public static void Wait(object obj) => Monitor.Wait(obj);

    public static void Wait(object obj, int millisecondsTimeout) => Monitor.Wait(obj, millisecondsTimeout);

    public static void Wait(object obj, TimeSpan timeout) => Monitor.Wait(obj, timeout);

    public static void Wait(object obj, int millisecondsTimeout, bool exitContext) => Monitor.Wait(obj, millisecondsTimeout, exitContext);

    public static void Wait(object obj, TimeSpan timeout, bool exitContext) => Monitor.Wait(obj, timeout, exitContext);
}

