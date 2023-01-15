namespace ChuckDeviceController.Common;

using System.ComponentModel.DataAnnotations;

//using System.Text.Json.Serialization;

//[JsonConverter(typeof(JsonStringEnumConverter))]
//public enum GeofenceType
//{
//    Circle,
//    Geofence,
//}

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

    public static implicit operator GeofenceType(string value)
    {
        return new GeofenceType(value);
    }

    public static implicit operator string(GeofenceType value)
    {
        return value._value;
    }

    public override string ToString()
    {
        return _value;
    }

    #endregion
}