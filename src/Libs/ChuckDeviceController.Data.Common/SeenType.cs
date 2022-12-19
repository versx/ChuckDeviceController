namespace ChuckDeviceController.Data.Common;

//[JsonConverter(typeof(JsonStringEnumConverter))]
//public enum SeenType
//{
//    [EnumMember(Value = "unset")]
//    Unset,

//    [EnumMember(Value = "encounter")]
//    Encounter,

//    [EnumMember(Value = "wild")]
//    Wild,

//    [EnumMember(Value = "nearby_stop")]
//    NearbyStop,

//    [EnumMember(Value = "nearby_cell")]
//    NearbyCell,

//    [EnumMember(Value = "lure_wild")]
//    LureWild,

//    [EnumMember(Value = "lure_encounter")]
//    LureEncounter,
//}

/// <summary>
/// Enumeration "emulator" since Dapper doesn't handle
/// type mapping with enums.
/// </summary>
public readonly struct SeenType
{
    private readonly string _value;

    #region Properties

    /// <summary>
    /// 
    /// </summary>
    public static SeenType Unset => "unset";

    /// <summary>
    /// Pokemon was seen by encountering
    /// </summary>
    public static SeenType Encounter => "encounter";

    /// <summary>
    /// Pokemon was seen in the wild
    /// </summary>
    public static SeenType Wild => "wild";

    /// <summary>
    /// Pokemon was seen near a Pokestop
    /// </summary>
    public static SeenType NearbyStop => "nearby_stop";

    /// <summary>
    /// Pokemon was seen near S2 cell
    /// </summary>
    public static SeenType NearbyCell => "nearby_cell";

    /// <summary>
    /// Pokemon was seen as wild lure spawn
    /// </summary>
    public static SeenType LureWild => "lure_wild";

    /// <summary>
    /// Pokemon was seen as lure encounter
    /// </summary>
    public static SeenType LureEncounter => "lure_encounter";

    #endregion

    #region Constructor

    private SeenType(string value)
    {
        _value = value;
    }

    #endregion

    #region Overrides

    public static implicit operator SeenType(string value)
    {
        return new SeenType(value);
    }

    public static implicit operator string(SeenType value)
    {
        return value._value;
    }

    public override string ToString()
    {
        return _value;
    }

    #endregion
}