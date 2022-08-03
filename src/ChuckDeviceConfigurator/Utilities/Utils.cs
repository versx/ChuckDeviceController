﻿namespace ChuckDeviceConfigurator.Utilities
{
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.Geometry.Models;

    public static class Utils
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
                ? (includeIcons ? "<i class=\"fa-solid fa-layer-group\"></i>&nbsp;" : null) + assignment.DeviceUuid
                : (includeIcons ? "<i class=\"fa-solid fa-mobile-screen-button\"></i>&nbsp;" : null) + assignment.DeviceGroupName;
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

        public static string FormatEnabled(bool enabled)
        {
            var status = enabled ? "Yes" : "No";
            var color = enabled ? "green" : "red";
            var displayText = $"<span style='color: {color}'>{status}</span>";
            //var displayText = $"<span class=\"{(enabled ? "webhook-enabled" : "webhook-disabled")}\">{status}</span>";
            return displayText;
        }

        public static string GetDeviceStatus(ulong lastSeen)
        {
            var now = DateTime.UtcNow.ToTotalSeconds();
            var status = now - lastSeen <= Strings.DeviceOnlineThresholdS
                ? Strings.DeviceOnlineIcon
                : Strings.DeviceOfflineIcon;
            return status;
        }

        public static string GetLastUpdatedStatus(ulong updated)
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
                : TimeSpanUtils.ToReadableString(updated);
            return updatedTime;
        }

        public static string GetAccountStatusColor(string status)
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

        public static string GetPokemonIcon(uint pokemonId, string width = "32", string height = "32", bool html = false)
        {
            var url = $"{Strings.PokemonImageUrl}/{pokemonId}.png";
            return html
                ? $"<img src='{url}' width='{width}' height='{height}' />"
                : url;
        }

        public static string GetGoogleMapsLink(double lat, double lon, bool html = false)
        {
            var link = string.Format(Strings.GoogleMapsLinkFormat, lat, lon);
            return html
                ? $"<a href='{link}'>{lat}, {lon}</a>"
                : link;
        }

        public static double BenchmarkAction(Action action, ushort precision = 4)
        {
            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            action();
            stopwatch.Start();

            var totalSeconds = Math.Round(stopwatch.Elapsed.TotalSeconds, precision);
            Console.WriteLine($"Benchmark took {totalSeconds}s");
            return totalSeconds;
        }

        public static int CompareCoordinates(Coordinate coord1, Coordinate coord2)
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
                var maxPagesBeforeCurrentPage = (int)Math.Floor((decimal)maxMiddlePage / (decimal)2);
                var maxPagesAfterCurrentPage = (int)Math.Ceiling((decimal)maxMiddlePage / (decimal)2) - 1;
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
    }
}