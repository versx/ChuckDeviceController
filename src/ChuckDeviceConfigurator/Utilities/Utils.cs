namespace ChuckDeviceConfigurator.Utilities
{
    using static POGOProtos.Rpc.PokemonDisplayProto.Types;

    using ChuckDeviceConfigurator.Services.Icons;
    using ChuckDeviceController.Common.Data;
    using ChuckDeviceController.Common.Geometry;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.Plugin;

    public static partial class Utils
    {
        public static string FormatAssignmentTime(uint timeS)
        {
            var times = TimeSpan.FromSeconds(timeS);
            return timeS == 0
                ? "On Complete"
                : $"{times.Hours:00}:{times.Minutes:00}:{times.Seconds:00}";
        }

        public static string FormatAssignmentText(Assignment assignment, bool includeIcons = false)
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

        public static string FormatInstanceType(InstanceType instanceType)
        {
            return instanceType switch
            {
                InstanceType.AutoQuest => "Auto Quest",
                InstanceType.Bootstrap => "Bootstrap",
                InstanceType.CirclePokemon => "Circle Pokemon",
                InstanceType.CircleRaid => "Circle Raid",
                InstanceType.Custom => "Custom",
                InstanceType.DynamicRoute => "Dynamic Route",
                InstanceType.FindTth => "Find TTH",
                InstanceType.Leveling => "Leveling",
                InstanceType.PokemonIV => "Pokemon IV",
                InstanceType.SmartRaid => "Smart Raid",
                _ => "Unknown",
            };
        }

        public static string GetDeviceStatus(ulong lastSeen)
        {
            var status = IsDeviceOnline(lastSeen)
                ? Strings.DeviceOnlineIcon
                : Strings.DeviceOfflineIcon;
            return status;
        }

        public static bool IsDeviceOnline(ulong lastSeen, uint onlineThresholdS = Strings.DeviceOnlineThresholdS)
        {
            var now = DateTime.UtcNow.ToTotalSeconds();
            var isOnline = now - lastSeen <= onlineThresholdS;
            return isOnline;
        }

        public static string GetLastUpdatedStatus(ulong updated, bool html = false)
        {
            var now = DateTime.UtcNow.ToTotalSeconds();
            var isMoreThanOneDay = now - updated > Strings.OneDayS;
            var lastUpdated = updated.FromSeconds()
                                     .ToLocalTime()
                                     .ToString(Strings.DefaultDateTimeFormat);
            var updatedTime = isMoreThanOneDay
                ? updated == 0
                    ? "Never"
                    : lastUpdated
                : updated.ToReadableString();

            var color = isMoreThanOneDay
                ? updated == 0
                    ? "orange"
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
                case "Invalid":
                case "Suspended":
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

        public static string GetPokemonIcon(uint pokemonId, uint formId = 0, Gender gender = Gender.Unset, uint costumeId = 0, string width = "32", string height = "32", bool html = false)
        {
            var url = UIconsService.Instance.GetPokemonIcon(pokemonId, formId, 0, gender, costumeId);
            return html
                ? $"<img src='{url}' width='{width}' height='{height}' />"
                : url;
        }

        public static string GetGoogleMapsLink(double lat, double lon, bool html = false)
        {
            var rndLat = Math.Round(lat, 5);
            var rndLon = Math.Round(lon, 5);
            var link = string.Format(Strings.GoogleMapsLinkFormat, lat, lon);
            return html
                ? $"<a href='{link}' target='_blank'>{rndLat},{rndLon}</a>"
                : link;
        }

        public static string GetQueueLink(string instanceName, string displayText = "Queue", string basePath = "/Instance/IvQueue", bool html = false)
        {
            var encodedName = Uri.EscapeDataString(instanceName);
            var url = $"{basePath}/{encodedName}";
            var status = $"<a href='{url}'>{displayText}</a>";
            return html
                ? status
                : url;
        }

        public static double BenchmarkAction(Action action, ushort precision = 4)
        {
            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            action();
            stopwatch.Start();

            var totalSeconds = Math.Round(stopwatch.Elapsed.TotalSeconds, precision);
            Console.WriteLine($"Benchmark took {totalSeconds}s for {action.Method.Name} (Target: {action.Target})");
            return totalSeconds;
        }

        public static int CompareCoordinates(ICoordinate coord1, ICoordinate coord2)
        {
            var d1 = Math.Pow(coord1.Latitude, 2) + Math.Pow(coord1.Longitude, 2);
            var d2 = Math.Pow(coord2.Latitude, 2) + Math.Pow(coord2.Longitude, 2);
            return d1.CompareTo(d2);
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

        public static async Task<Dictionary<SettingsPropertyGroup, List<SettingsProperty>>> GroupPropertiesAsync(List<SettingsProperty> properties)
        {
            var dict = new Dictionary<SettingsPropertyGroup, List<SettingsProperty>>();
            foreach (var property in properties)
            {
                var group = property.Group ?? new();
                if (!dict.ContainsKey(group))
                {
                    dict.Add(group, new() { property });
                }
                else
                {
                    dict[group].Add(property);
                    dict[group].Sort((a, b) => a.DisplayIndex.CompareTo(b.DisplayIndex));
                }
            }
            return await Task.FromResult(dict);
        }
    }
}