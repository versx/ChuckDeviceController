namespace ChuckDeviceController.Plugin;

using ChuckDeviceController.Common;

/// <summary>
///     Sets the expected <seealso cref="GeofenceType"/> for custom
///     <seealso cref="IJobControllerServiceHost"/> instances.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
//[PropertyValidation(typeof(GeofenceType))]
public sealed class GeofenceTypeAttribute : Attribute
{
    /// <summary>
    ///     Gets the specified geofence type expected.
    /// </summary>
    public GeofenceType Type { get; set; }

    /// <summary>
    ///     Instantiates a new instance of the <see cref="GeofenceTypeAttribute"/>
    ///     attribute class.
    /// </summary>
    /// <param name="type">
    ///     Expected geofence type required by the custom <seealso cref="Common.Jobs.IJobController"/>.
    /// </param>
    public GeofenceTypeAttribute(string type)
    {
        Type = type;
    }
}