namespace Dapper.Tests;

using ChuckDeviceController.Data.Common;
using ChuckDeviceController.Data.Entities;
using ChuckDeviceController.Data.Factories;
using ChuckDeviceController.Extensions.Json;
using ChuckDeviceController.Extensions.Json.Converters;

internal class GeofenceTests
{

    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void TestGeofenceData()
    {
        var json = """
{
  "name": "TestGeofence",
  "type": "geofence",
  "data":
  {
    "area": [
      {
        "lat": 34.01,
        "lon": -117.01
      },
      {
        "lat": 34.02,
        "lon": -117.02
      },
      {
        "lat": 34.03,
        "lon": -117.03
      },
      {
        "lat": 34.04,
        "lon": -117.04
      }
    ]
  }
}
""";

        var geofence = json.FromJson<Geofence>(new[] { new ObjectDataConverter<GeofenceData>() });//DbContextFactory.JsonDictionaryConverters);
        var area = geofence!.Data!.Area;
        var area2 = geofence!.Data["area"];

        Assert.Multiple(() =>
        {
            Assert.That(area == area2, Is.True);
            Assert.That(geofence, Is.Not.Null);
        });
    }
}