namespace Dapper.Tests;

using ChuckDeviceController.Common;
using ChuckDeviceController.Data.Entities;
using ChuckDeviceController.Extensions.Json;
using ChuckDeviceController.Extensions.Json.Converters;

internal class InstanceTests
{

    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void TestInstanceData()
    {
        var json = """
{
  "name": "TestInstance",
  "type": "custom",
  "min_level": 30,
  "max_level": 39,
  "geofences": ["TestGeofence"],
  "data":
  {
    "circle_route_type": "Smart",
    "optimize_dynamic_route": false,
    "timezone": null,
    "enable_dst": null,
    "spin_limit": null,
    "ignore_s2_cell_bootstrap": null,
    "use_warning_accounts": null,
    "quest_mode": "Normal",
    "max_spin_attempts": 0,
    "logout_delay": 0,
    "iv_queue_limit": null,
    "iv_list": null,
    "enable_lure_encounters": null,
    "fast_bootstrap_mode": null,
    "circle_size": null,
    "optimize_bootstrap_route": false,
    "bootstrap_complete_instance_name": null,
    "optimize_spawnpoints_route": false,
    "only_unknown_spawnpoints": false,
    "leveling_radius": 0,
    "store_leveling_data": false,
    "leveling_start_coordinate": null,
    "account_group": null,
    "is_event": null,
    "custom_instance_type": "test_controller"
  }
}
""";

        var instance = json.FromJson<Instance>(new[] { new ObjectDataConverter<InstanceData>() });//DbContextFactory.JsonDictionaryConverters);
        var circleRouteType = instance!.Data!.CircleRouteType;
        var circleRouteType2 = instance!.Data["circle_route_type"];//.Get<CircleInstanceRouteType>("circle_route_type");
        //var test = instance!.Get<string>("custom_instance_type", null);

        Assert.Multiple(() =>
        {
            Assert.That(circleRouteType.ToString(), Is.EqualTo(circleRouteType2?.ToString()));
            Assert.That(instance, Is.Not.Null);
        });
    }
}

public static class DictionaryExtensions
{
    public static T? Get<T>(this Dictionary<string, object> dictionary, string key, T? defaultValue = default)
    {
        return dictionary.TryGetValue(key, out var value)
            ? (T?)value
            : defaultValue;
    }
}