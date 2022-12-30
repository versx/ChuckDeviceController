namespace Tests;

using ChuckDeviceController.Data.Common;

internal class BitwiseTests
{
    [SetUp]
    public void SetUp()
    {
    }

    [Test]
    public void TestBitwiseValues()
    {
        foreach (var flag in Enum.GetValues<PluginApiKeyScope>())
        {
            var dec = (int)flag;
            var binary = Convert.ToString(dec, 2);
            Console.WriteLine($"{flag} [Binary: {binary}, Decimal: {dec}]");
        }
    }
}