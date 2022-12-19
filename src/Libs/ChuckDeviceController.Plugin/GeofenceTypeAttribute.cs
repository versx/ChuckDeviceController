namespace ChuckDeviceController.Plugin;

using ChuckDeviceController.Data.Common;

/// <summary>
/// 
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class GeofenceTypeAttribute : Attribute
{
    /// <summary>
    /// 
    /// </summary>
    public GeofenceType Type { get; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="type"></param>
    public GeofenceTypeAttribute(GeofenceType type)
    {
        Type = type;
    }
}