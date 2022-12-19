namespace ChuckDeviceController.JobControllers;

public sealed class AccountLevelUpEventArgs : EventArgs
{
    public ushort Level { get; }

    public string Username { get; }

    public ulong XP { get; }

    public ulong DateReached { get; }

    public AccountLevelUpEventArgs(ushort level, string username, ulong xp, ulong dateReached)
    {
        Level = level;
        Username = username;
        XP = xp;
        DateReached = dateReached;
    }
}