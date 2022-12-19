namespace ChuckDeviceController.Data.TypeHandlers;

using System.Data;

using Dapper;

using ChuckDeviceController.Extensions.Json;

public class JsonTypeHandler<T> : SqlMapper.TypeHandler<T>
{
    public override T Parse(object value)
    {
        var json = value?.ToString();
        if (string.IsNullOrEmpty(json))
        {
            return default!;
        }
        var obj = json.FromJson<T>() ?? default;
        return obj ?? default!;
    }

    public override void SetValue(IDbDataParameter parameter, T value)
    {
        var json = value?.ToJson();
        parameter.Value = json ?? null;
    }
}