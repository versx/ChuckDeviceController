namespace ChuckDeviceController.Common
{
    public interface IAccount
    {
        string Username { get; }

        ushort Level { get; }

        double? LastEncounterLatitude { get; }

        double? LastEncounterLongitude { get; }

        ulong? LastEncounterTime { get; }
    }
}