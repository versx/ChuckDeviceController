namespace ChuckDeviceController.Services;

public class ThreadManager
{
    private readonly int _maxThreadCount;
    private int _threadsUsed;

    public int ThreadsUsed => _threadsUsed;

    public bool IsThreadAvailable => _threadsUsed < _maxThreadCount;

    public ThreadManager(int maxThreadCount = 100)
    {
        _maxThreadCount = maxThreadCount;
    }

    public void TakeThread()
    {
        if (_threadsUsed < _maxThreadCount)
        {
            Interlocked.Increment(ref _threadsUsed);
        }
    }

    public void GiveThread()
    {
        if (_threadsUsed <= _maxThreadCount)
        {
            Interlocked.Decrement(ref _threadsUsed);
        }
    }
}