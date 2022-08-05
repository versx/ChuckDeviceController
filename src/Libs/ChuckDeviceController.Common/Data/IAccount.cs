namespace ChuckDeviceController.Common.Data
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