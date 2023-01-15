namespace ChuckDeviceController.Data.TypeHandlers;

using System.Data;

using Dapper;

using ChuckDeviceController.Common;

public class GeofenceTypeTypeHandler : SqlMapper.ITypeHandler
{
    public static readonly GeofenceTypeTypeHandler Default = new();

    public object Parse(Type destinationType, object value)
    {
        if (destinationType == typeof(GeofenceType))
        {
            return (GeofenceType)(string)value;
        }
        return GeofenceType.Circle;
    }

    public void SetValue(IDbDataParameter parameter, object value)
    {
        parameter.DbType = DbType.String;
        parameter.Value = (string)(dynamic)value;
    }
}