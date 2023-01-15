namespace ChuckDeviceController.Data.TypeHandlers;

using System.Data;

using Dapper;

using ChuckDeviceController.Common;

public class InstanceTypeTypeHandler : SqlMapper.TypeHandler<InstanceType>
{
    public static readonly InstanceTypeTypeHandler Default = new();

    public override InstanceType Parse(object value)
    {
        var val = value?.ToString();
        if (string.IsNullOrEmpty(val))
        {
            return default!;
        }
        var instanceType = InstanceType.StringToInstanceType(val);
        return instanceType;
    }

    public override void SetValue(IDbDataParameter parameter, InstanceType value)
    {
        parameter.Value = InstanceType.InstanceTypeToString(value) ?? null;
    }
}