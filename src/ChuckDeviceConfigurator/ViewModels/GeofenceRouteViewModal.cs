namespace ChuckDeviceConfigurator.ViewModels;

public class GeofenceRouteViewModal
{
    private const string DefaultFormat = "json";

    public string Title { get; set; }

    public string Format { get; set; }

    public string Geofence { get; set; }

    public string FormatChangedMethod { get; set; }

    public string SubmitButtonText { get; set; }

    public string SubmitButtonMethod { get; set; }

    public GeofenceRouteViewModal()
    {
        Title = string.Empty;
        Format = DefaultFormat;
        Geofence = string.Empty;
        FormatChangedMethod = string.Empty;
        SubmitButtonText = string.Empty;
        SubmitButtonMethod = string.Empty;
    }

    public GeofenceRouteViewModal(string title, string format = DefaultFormat,
        string? geofence = null, string? formatChangedMethod = null,
        string? submitButtonText = null, string? submitButtonMethod = null)
    {
        Title = title;
        Format = format;
        Geofence = geofence ?? string.Empty;
        FormatChangedMethod = formatChangedMethod ?? string.Empty;
        SubmitButtonText = submitButtonText ?? string.Empty;
        SubmitButtonMethod = submitButtonMethod ?? string.Empty;
    }
}