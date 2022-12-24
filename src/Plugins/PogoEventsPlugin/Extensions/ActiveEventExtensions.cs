namespace PogoEventsPlugin.Extensions;

using ChuckDeviceController.Extensions;

using Models;

public static class ActiveEventExtensions
{
    public static IEnumerable<IActiveEvent> Filter(this IEnumerable<IActiveEvent> events, bool active = false, bool sorted = false)
    {
        var results = new List<IActiveEvent>();
        if (active)
        {
            // Now timestamp in seconds
            var now = DateTime.UtcNow.ToTotalSeconds();
            // Filter for only active evnets within todays date
            results = events
                .Where(evt => DateTime.Parse(evt.Start).ToTotalSeconds() <= now && now < DateTime.Parse(evt.End).ToTotalSeconds())
                .ToList();

            // Check if no active events available
            if (!results.Any())
            {
                // No active events, return empty list instead of all events
                // because 'active' param was true.
                return results;
            }
        }

        if (sorted)
        {
            // Sort active events by end date
            results.Sort((a, b) => DateTime.Parse(a.End).CompareTo(DateTime.Parse(b.End)));
        }
        return results;
    }

    public static string FormatEventName(this IActiveEvent activeEvent, string channelNameFormat = "{0}-{1} {2}")
    {
        var eventEndDate = //activeEvent.End != null
            DateTime.Parse(activeEvent.End);
        //: "N/A";
        var channelName = string.Format(channelNameFormat, eventEndDate.Month, eventEndDate.Day, activeEvent.Name);
        return channelName;
    }
}