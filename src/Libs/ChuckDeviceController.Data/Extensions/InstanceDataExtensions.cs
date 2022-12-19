namespace ChuckDeviceController.Data.Extensions;

using ChuckDeviceController.Data.Common;

public static class InstanceDataExtensions
{
    public static TValue? GetValue<TValue>(this InstanceData data, string key, TValue? defaultValue = default)
        where TValue : class
    {
        data.TryGetValue(key, out var value);
        return (value ?? defaultValue) as TValue;
    }
}