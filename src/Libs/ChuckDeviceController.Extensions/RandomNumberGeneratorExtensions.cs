namespace ChuckDeviceController.Extensions;

using System.Security.Cryptography;

public static class RandomNumberGeneratorExtensions
{
    public static int Next(this RandomNumberGenerator generator, int min, int max)
    {
        // match Next of Random
        // where max is exclusive
        max--;

        var bytes = new byte[sizeof(int)]; // 4 bytes
        generator.GetNonZeroBytes(bytes);
        var val = BitConverter.ToInt32(bytes);
        // constrain our values to between our min and max
        // https://stackoverflow.com/a/3057867/86411
        var result = ((val - min) % (max - min + 1) + (max - min + 1)) % (max - min + 1) + min;
        return result;
    }
}