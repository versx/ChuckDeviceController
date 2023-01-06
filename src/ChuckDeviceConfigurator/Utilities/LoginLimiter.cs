namespace ChuckDeviceConfigurator.Utilities;

using Microsoft.Extensions.Options;

using ChuckDeviceController.Configuration;
using ChuckDeviceController.Extensions;

public interface ILoginLimiter
{
    LoginLimitConfig Options { get; }

    Task<(bool, ulong)> IsLimitedAsync(string host);
}

public class LoginLimiter : ILoginLimiter
{
    public const uint DefaultLoginLimit = 15;
    public const uint DefaultLoginIntervalS = 300;

    private readonly Dictionary<string, ulong> _loginLimitTime = new();
    private readonly Dictionary<string, ulong> _loginLimitCount = new();
    private readonly SemaphoreSlim _sem = new(1, 1);

    public LoginLimitConfig Options { get; }

    public LoginLimiter(IOptions<LoginLimitConfig> options)
    {
        Options = options.Value;
    }

    public async Task<(bool, ulong)> IsLimitedAsync(string host)
    {
        var now = DateTime.UtcNow.ToTotalSeconds();
        var currentTime = now / Options.IntervalS;
        var left = Options.IntervalS - now % Options.IntervalS;

        AddOrUpdateDevice(host);

        await _sem.WaitAsync().ConfigureAwait(false);

        ulong currentCount;
        if (_loginLimitTime[host] != currentTime)
        {
            _loginLimitTime[host] = currentTime;
            currentCount = 0;
        }
        else
        {
            currentCount = _loginLimitCount[host];
        }

        if (currentCount >= Options.MaximumCount)
        {
            _sem.Release();
            return (true, left);
        }
        _loginLimitCount[host] = currentCount + 1;

        _sem.Release();
        return (false, left);
    }

    private void AddOrUpdateDevice(string host)
    {
        if (!_loginLimitTime.TryGetValue(host, out var _))
        {
            _loginLimitTime.Add(host, 0);
        }
        if (!_loginLimitCount.TryGetValue(host, out var _))
        {
            _loginLimitCount.Add(host, 0);
        }
    }
}