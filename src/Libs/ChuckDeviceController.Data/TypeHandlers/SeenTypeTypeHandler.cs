namespace ChuckDeviceController.Data.TypeHandlers
{
    using System.Data;

    using Dapper;

    using ChuckDeviceController.Common.Data;

    public class SeenTypeTypeHandler : SqlMapper.ITypeHandler
    {
        public static readonly SeenTypeTypeHandler Default = new();

        public object Parse(Type destinationType, object value)
        {
            if (destinationType == typeof(SeenType))
            {
                return (SeenType)(string)value;
            }
            return null;
        }

        public void SetValue(IDbDataParameter parameter, object value)
        {
            parameter.DbType = DbType.String;
            parameter.Value = (string)(dynamic)value;
        }
    }
}