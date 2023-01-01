namespace RobotsPlugin.Utilities;

public static class Utils
{
    public static bool IsEqual(string a, string b)
    {
        return a.Equals(b, StringComparison.InvariantCultureIgnoreCase);
    }
}