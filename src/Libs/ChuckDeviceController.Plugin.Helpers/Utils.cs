namespace ChuckDeviceController.Plugin.Helpers;

using System.Diagnostics;

using ChuckDeviceController.Common;
using ChuckDeviceController.Common.Abstractions;
using ChuckDeviceController.Extensions;

public static class Utils
{
    public const ushort TenMinutesS = 600; // 10 minutes
    public const ushort ThirtyMinutesS = TenMinutesS * 3; // 30 minutes (1800)
    public const ushort SixtyMinutesS = ThirtyMinutesS * 2; // 60 minutes (3600)
    public const uint OneDayS = SixtyMinutesS * 24; // 24 hours (86400)
    public const ushort DeviceOnlineThresholdS = 15 * 60; // 15 minutes (900)
    public const string DeviceOnlineIcon = "🟢"; // green dot
    public const string DeviceOfflineIcon = "🔴"; // red dot
    public const string PokemonImageUrl = "https://raw.githubusercontent.com/WatWowMap/wwm-uicons/main/pokemon/";
    public const string GoogleMapsLinkFormat = "https://maps.google.com/maps?q={0},{1}";
    public const string DefaultDateTimeFormat = "MM/dd/yyyy hh:mm:ss tt";

    public static string FormatBenchmarkTime(double timeS, bool isHtml = false)
    {
        var color = timeS <= 3
            ? "green"
            : timeS > 3 && timeS < 10
                ? "orange"
                : "red";
        return isHtml
            ? $"<span style='color: {color}'>{timeS}</span>"
            : timeS.ToString();
    }

    public static string FormatAssignmentTime(uint timeS)
    {
        var times = TimeSpan.FromSeconds(timeS);
        return timeS == 0
            ? "On Complete"
            : $"{times.Hours:00}:{times.Minutes:00}:{times.Seconds:00}";
    }

    public static string FormatAssignmentText(IAssignment assignment, bool includeIcons = false)
    {
        var sourceInstance = string.IsNullOrEmpty(assignment.SourceInstanceName)
            ? null
            : assignment.SourceInstanceName;
        var deviceOrGroupName = string.IsNullOrEmpty(assignment.DeviceGroupName)
            ? (includeIcons ? "<i class=\"fa-solid fa-fw fa-mobile-screen-button\"></i>&nbsp;" : null) + assignment.DeviceUuid
            : (includeIcons ? "<i class=\"fa-solid fa-fw fa-layer-group\"></i>&nbsp;" : null) + assignment.DeviceGroupName;
        var time = FormatAssignmentTime(assignment.Time);

        var sb = new System.Text.StringBuilder();
        sb.Append(deviceOrGroupName);
        if (!string.IsNullOrEmpty(sourceInstance))
        {
            sb.Append($" (From: {sourceInstance})");
        }
        sb.Append(" -> ");
        sb.Append(assignment.InstanceName);
        sb.Append($" ({time})");
        var displayText = sb.ToString();
        return displayText;
    }

    public static string FormatBoolean(bool isTrue, bool html = false)
    {
        var status = isTrue ? "Yes" : "No";
        if (!html)
        {
            return status;
        }
        var color = isTrue ? "green" : "red";
        var displayText = $"<span style='color: {color}'>{status}</span>";
        return displayText;
    }

    public static string FormatNull(string? value, string defaultValue = "N/A")
    {
        if (string.IsNullOrEmpty(value))
        {
            return defaultValue;
        }
        return value;
    }

    public static string FormatInstanceType(InstanceType instanceType, InstanceData? data)
    {
        if (instanceType == InstanceType.Custom)
        {
            return "Custom" + (data == null ? "" : $" ({data.CustomInstanceType})");
        }

        return instanceType;
    }

    public static string GetDeviceStatus(ulong lastSeen)
    {
        var status = IsDeviceOnline(lastSeen)
            ? DeviceOnlineIcon
            : DeviceOfflineIcon;
        return status;
    }

    public static bool IsDeviceOnline(ulong lastSeen, uint onlineThresholdS = DeviceOnlineThresholdS)
    {
        var now = DateTime.UtcNow.ToTotalSeconds();
        var isOnline = now - lastSeen <= onlineThresholdS;
        return isOnline;
    }

    public static string GetLastUpdatedStatus(ulong updated, bool html = false)
    {
        var now = DateTime.UtcNow.ToTotalSeconds();
        var isMoreThanOneDay = now - updated > OneDayS;
        var lastUpdated = updated
            .FromSeconds()
            .ToLocalTime()
            .ToString(DefaultDateTimeFormat);
        var updatedTime = isMoreThanOneDay
            ? updated == 0
                ? "Never"
                : lastUpdated
            : updated.ToReadableString();

        var color = isMoreThanOneDay
            ? updated == 0
                ? "inherit"
                : "red"
            : "green";
        return html
            ? $"<span style='color: {color};'>{updatedTime}</span>"
            : updatedTime;
    }

    public static string GetAccountStatusColor(string? status)
    {
        var cssClass = "text-dark";
        switch (status)
        {
            case "Good":
                cssClass = "account-good";
                break;
            case "Banned":
                cssClass = "account-banned";
                break;
            case "Warning":
            case "Suspended":
            case "Invalid":
                cssClass = "account-warning";
                break;
        }
        var html = "<span class='{0}'>{1}</span>";
        return string.Format(html, cssClass, status);
    }

    public static string GetPluginStateColor(PluginState state)
    {
        var color = "black";
        switch (state)
        {
            case PluginState.Running:
                color = "green";
                break;
            case PluginState.Stopped:
            case PluginState.Disabled:
            case PluginState.Error:
                color = "red";
                break;
            case PluginState.Removed:
                color = "blue";
                break;
            case PluginState.Unset: // should never hit
            default:
                break;
        }
        var html = $"<span style='color: {color};'>{state}</span>";
        return html;
    }

    public static string GetGoogleMapsLink(double lat, double lon, bool html = false)
    {
        var rndLat = Math.Round(lat, 5);
        var rndLon = Math.Round(lon, 5);
        var link = string.Format(GoogleMapsLinkFormat, lat, lon);
        return html
            ? $"<a href='{link}' target='_blank'>{rndLat},{rndLon}</a>"
            : link;
    }

    public static double BenchmarkAction(Action action, ushort precision = 4)
    {
        var sw = new Stopwatch();
        sw.Start();
        action();
        sw.Stop();

        var totalSeconds = Math.Round(sw.Elapsed.TotalSeconds, precision);
        Console.WriteLine($"Benchmark took {totalSeconds}s for {action.Method.Name} (Target: {action.Target})");
        return totalSeconds;
    }

    // Credits: https://jasonwatmore.com/post/2018/10/17/c-pure-pagination-logic-in-c-aspnet
    public static List<int> GetNextPages(int page, int maxPages)
    {
        int startPage, endPage, maxMiddlePage = 5;
        if (maxPages <= maxMiddlePage)
        {
            // Total pages less than max so show all pages
            startPage = 1;
            endPage = maxPages;
        }
        else
        {
            var maxPagesBeforeCurrentPage = (int)Math.Floor((decimal)maxMiddlePage / 2);
            var maxPagesAfterCurrentPage = (int)Math.Ceiling((decimal)maxMiddlePage / 2) - 1;
            if (page <= maxPagesBeforeCurrentPage)
            {
                // Current page near the start
                startPage = 1;
                endPage = maxMiddlePage;
            }
            else if (page + maxPagesAfterCurrentPage >= maxPages)
            {
                // Current page near the end
                startPage = maxPages - maxMiddlePage + 1;
                endPage = maxPages;
            }
            else
            {
                // Current page somewhere in the middle
                startPage = page - maxPagesBeforeCurrentPage;
                endPage = page + maxPagesAfterCurrentPage;
            }
        }

        // Create an array of pages that can be looped over
        var result = Enumerable.Range(startPage, (endPage + 1) - startPage).ToList();
        return result;
    }

    public static bool HasProperty(dynamic obj, string name)
    {
        var objType = obj.GetType();

        if (objType == typeof(System.Dynamic.ExpandoObject))
        {
            return ((IDictionary<string, object>)obj).ContainsKey(name);
        }

        var result = objType.GetProperty(name) != null;
        return result;
    }
}