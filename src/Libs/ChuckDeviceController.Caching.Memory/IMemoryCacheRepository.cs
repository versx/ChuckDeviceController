namespace ChuckDeviceController.Caching.Memory;

public interface IMemoryCacheRepository
{
    bool IsSet(string key);

    bool TryGetValue<T>(string key, out T? value);

    T? GetValue<T>(string key);

    void SetValue<T>(string key, T value, ushort expiryS);

    void Remove(string key);

    void Clear();
}