namespace Tests;

using ChuckDeviceController.Extensions.Json;
using ChuckDeviceController.Geometry.Models;
using ChuckDeviceController.Net.Models;
using ChuckDeviceController.Net.Models.Requests.Koji;
using ChuckDeviceController.Net.Models.Responses.Koji;
using MultiPolygon = ChuckDeviceController.Net.Models.Responses.Koji.MultiPolygon;
using ChuckDeviceController.Net.Utilities;

internal class KojiTests
{
    private const string BaseUrl = "http://10.0.0.7:8080/api/v1";
    private const string GeofenceProjectUrl = BaseUrl + "/geofence/{0}/{1}";
    private const string AllGeofencesUrl = BaseUrl + "/geofence/{0}";

    [SetUp]
    public void SetUp()
    {
    }

    [TestCase("Test")]
    public async Task TestFetchGeofences(string projectName)
    {
        var bearer = "";
        var url = GeofenceProjectUrl;

        var request = await NetUtils.GetAsync(string.Format(url, KojiReturnType.Text, projectName), bearer);
        var textResponse = request?.FromJson<KojiApiResponse<string>>();
        Console.WriteLine($"Text Response: {textResponse}");
        Assert.That(textResponse is not null, Is.True);

        request = await NetUtils.GetAsync(string.Format(url, KojiReturnType.AltText, projectName), bearer);
        var altTextResponse = request?.FromJson<KojiApiResponse<string>>();
        Console.WriteLine($"AltText Response: {altTextResponse}");
        Assert.That(altTextResponse is not null, Is.True);

        request = await NetUtils.GetAsync(string.Format(url, KojiReturnType.SingleArray, projectName), bearer);
        var singleArrayResponse = request?.FromJson<KojiApiResponse<MultiPolygon>>();
        Console.WriteLine($"SingleArray Response: {singleArrayResponse}");
        Assert.That(singleArrayResponse is not null, Is.True);

        request = await NetUtils.GetAsync(string.Format(url, KojiReturnType.MultiArray, projectName), bearer);
        var multiArrayResponse = request?.FromJson<KojiApiResponse<List<MultiPolygon>>>();
        Console.WriteLine($"MultiArray Response: {multiArrayResponse}");
        Assert.That(multiArrayResponse is not null, Is.True);

        request = await NetUtils.GetAsync(string.Format(url, KojiReturnType.SingleStruct, projectName), bearer);
        var singleStructResponse = request?.FromJson<KojiApiResponse<List<Coordinate>>>();
        Console.WriteLine($"SingleStruct Response: {singleStructResponse}");
        Assert.That(singleStructResponse is not null, Is.True);

        request = await NetUtils.GetAsync(string.Format(url, KojiReturnType.MultiStruct, projectName), bearer);
        var multiStructResponse = request?.FromJson<KojiApiResponse<List<List<Coordinate>>>>();
        Console.WriteLine($"MultiStruct Response: {multiStructResponse}");
        Assert.That(multiStructResponse is not null, Is.True);

        request = await NetUtils.GetAsync(string.Format(url, KojiReturnType.Feature, projectName), bearer);
        var featureResponse = request?.FromJson<KojiApiResponse<KojiFeatureDataResponse>>();
        Console.WriteLine($"Feature Response: {featureResponse}");
        Assert.That(featureResponse is not null, Is.True);

        request = await NetUtils.GetAsync(string.Format(url, KojiReturnType.FeatureVec, projectName), bearer);
        var featureVecResponse = request?.FromJson<KojiApiResponse<List<KojiFeatureVecDataResponse>>>();
        Console.WriteLine($"FeatureVec Response: {featureVecResponse}");
        Assert.That(featureVecResponse is not null, Is.True);

        request = await NetUtils.GetAsync(string.Format(url, KojiReturnType.FeatureCollection, projectName), bearer);
        var featureCollectionResponse = request?.FromJson<KojiApiResponse<KojiFeatureCollectionDataResponse>>();
        Console.WriteLine($"FeatureCollection Response: {featureCollectionResponse}");
        Assert.That(featureCollectionResponse is not null, Is.True);

        request = await NetUtils.GetAsync(string.Format(url, KojiReturnType.Poracle, projectName), bearer);
        var poracleResponse = request?.FromJson<KojiApiResponse<List<KojiPoracleDataResponse>>>();
        Console.WriteLine($"Poracle Response: {poracleResponse}");
        Assert.That(poracleResponse is not null, Is.True);

        url = AllGeofencesUrl;
        request = await NetUtils.GetAsync(string.Format(url, "geojson"), bearer);
        var allGeofencesResponse = request?.FromJson<KojiApiResponse<KojiGeofencesDataResponse>>();
        Console.WriteLine($"All Geofences Response: {allGeofencesResponse}");
        Assert.That(allGeofencesResponse is not null, Is.True);
    }
}