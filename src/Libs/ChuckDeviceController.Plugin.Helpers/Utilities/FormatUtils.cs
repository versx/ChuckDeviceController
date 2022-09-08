namespace ChuckDeviceController.Plugin.Helpers.Utilities
{
    using ChuckDeviceController.Common.Data;
    using ChuckDeviceController.Common.Data.Contracts;

    public static class FormatUtils
    {
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
    }
}