namespace ChuckDeviceController.Common;

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

    public static implicit operator SeenType(string value) => new(value);

    public static implicit operator string(SeenType value) => value._value;

    public override string ToString() => _value;

    #endregion

    #region Helper Methods

    public static string SeenTypeToString(SeenType type) => type.ToString();

    public static SeenType StringToSeenType(string seenType) => (SeenType)seenType;

    #endregion
}