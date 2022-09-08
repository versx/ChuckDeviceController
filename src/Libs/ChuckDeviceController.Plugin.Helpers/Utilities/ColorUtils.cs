namespace ChuckDeviceController.Plugin.Helpers.Utilities
{
    using ChuckDeviceController.Common.Data;

    public static class ColorUtils
    {
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
    }
}