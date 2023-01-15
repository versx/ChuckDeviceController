namespace ChuckDeviceController.Common;

using System.ComponentModel.DataAnnotations;

public readonly struct GeofenceType
{
    private readonly string _value;

    #region Properties

    public static IEnumerable<GeofenceType> Values => new[]
    {
        Circle,
        Geofence,
    };

    [Display(GroupName = "", Name = "Circle", Description = "")]
    public static GeofenceType Circle => "Circle"; //circle

    [Display(GroupName = "", Name = "Geofence", Description = "")]
    public static GeofenceType Geofence => "Geofence"; //geofence


    #endregion

    #region Constructor

    private GeofenceType(string value)
    {
        _value = value;
    }

    #endregion

    #region Overrides

    public static implicit operator GeofenceType(string value) => new(value);

    public static implicit operator string(GeofenceType value) => value._value;

    public override string ToString() => _value;

    #endregion

    #region Helper Methods

    public static string GeofenceTypeToString(GeofenceType type) => type.ToString();

    public static GeofenceType StringToGeofenceType(string geofenceType) => (GeofenceType)geofenceType;

    #endregion
}