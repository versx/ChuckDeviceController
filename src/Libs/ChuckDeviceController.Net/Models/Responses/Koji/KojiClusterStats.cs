namespace ChuckDeviceController.Net.Models.Responses.Koji;

using System.Text.Json.Serialization;

public class KojiClusterStats
{
    [JsonPropertyName("best_clusters")]
    public object? BestClusters { get; set; } // TODO: List?

    [JsonPropertyName("best_cluster_point_count")]
    public uint BestClusterPointCount { get; set; }

    [JsonPropertyName("cluster_time")]
    public double ClusterTime { get; set; }

    [JsonPropertyName("total_points")]
    public uint TotalPoints { get; set; }

    [JsonPropertyName("points_covered")]
    public uint PointsCovered { get; set; }

    [JsonPropertyName("total_clusters")]
    public uint TotalClusters { get; set; }

    [JsonPropertyName("total_distance")]
    public double TotalDistance { get; set; }

    [JsonPropertyName("longest_distance")]
    public double LongestDistance { get; set; }
}