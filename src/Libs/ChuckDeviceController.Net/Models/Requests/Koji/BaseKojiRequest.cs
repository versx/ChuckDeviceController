namespace ChuckDeviceController.Net.Models.Requests.Koji;

using System.Text.Json.Serialization;

public class BaseKojiRequest
{
    [JsonPropertyName("return_type")]
    public string? Area { get; set; }

    [JsonPropertyName("return_type")]
    public string? Instance { get; set; }

    [JsonPropertyName("return_type")]
    public KojiReturnType? ReturnType { get; set; }
}

/** All API Fields
pub struct Args {
    // The instance or area to lookup in the db to get geofence/data points
    // defaults to ""
    pub instance: Option<String>,

    // radius of the circle to use in calculations
    // defaults to 70m
    pub radius: Option<f64>,

    // min number of points to use with clustering
    // defaults to 1
    pub min_points: Option<usize>,

    // number of times to run through the clustering optimizations
    // defaults to 1
    pub generations: Option<usize>,

    // number of seconds (s) to run the routing algorithm (longer = better routes)
    // defaults to 1
    pub routing_time: Option<i64>,

    // number of devices - not implemented atm
    // defaults to 1
    pub devices: Option<usize>,

    // Custom list of data points to use in calculations - overrides all else
    // defaults to []
    pub data_points: Option<DataPointsArg>,

    // Custom area to use in the SQL query to get data points
    // defaults to empty FeatureCollection
    pub area: Option<GeoFormats>,

    // Run the fast algorithm or not
    // defaults to true
    pub fast: Option<bool>,

    // Format of how the data should be returned
    // defaults to AreaInput type or SingleArray if AreaInput is None
    pub return_type: Option<String>,

    // Only return stats
    // defaults to false
    pub benchmark_mode: Option<bool>,

    // Only count unique points towards the min_count in each cluster
    // defaults to false
    pub only_unique: Option<bool>,

    // Filter spawnpoints by `last_seen` and pokestops/gyms by `updated`
    // defaults to 0
    pub last_seen: Option<i64>,

    // Auto save the results to the scanner database
    // defaults to false
    pub save_to_db: Option<bool>,

    // Number of points to split by when routing
    // Lower = better local routing but may have longer stretches that join the smaller routes
    // defaults to 250
    pub route_chunk_size: Option<usize>
}
 */