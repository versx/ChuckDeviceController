namespace PogoEventsPlugin.Utilities
{
    using Models;

    using ChuckDeviceController.Plugin;

    public static class Utils
    {
        public static string FormatEventItems(IEnumerable<IEventItem> items)
        {
            return string.Join("<br>", items.Select(item => FormatUpper(item.Template)));
        }

        public static string FormatEventRaidItems(IEnumerable<IEventRaidItem> raids, ILocalizationHost localeHost)
        {
            var items = raids.Select(raid =>
            {
                var name = FormatUpper(raid.Template);
                var formName = localeHost.GetFormName(raid.Form ?? 0, true);
                return raid.Form > 0
                    ? $"{name} ({formName})"
                    : name;
            });
            return string.Join("<br>", items);
        }

        public static string FormatEventBonusItems(IEnumerable<IEventBonusItem> bonuses)
        {
            return string.Join("<br>", bonuses.Select(bonus => bonus.Text));
        }

        public static string FormatUpper(string text)
        {
            var value = text[1..^1].ToLower();
            var firstChar = text[0].ToString().ToUpper();
            return firstChar + value;
        }
    }
}