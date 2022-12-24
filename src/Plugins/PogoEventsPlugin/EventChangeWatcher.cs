namespace PogoEventsPlugin;

using ChuckDeviceController.Extensions.Json;
using ChuckDeviceController.Net.Utilities;

using Models;

public class EventChangeWatcher
{
    private const int IntervalM = 1; // 5 minutes

    #region Variables

    private readonly string _url;
    private readonly System.Timers.Timer _timer;
    private string? _previousData;

    #endregion

    #region Events

    public event EventHandler<EventChangedEventArgs>? Changed;
    private void OnChanged(IEnumerable<IActiveEvent> activeEvents)
    {
        Changed?.Invoke(this, new EventChangedEventArgs(activeEvents));
    }

    #endregion

    #region Constructors

    public EventChangeWatcher(string url)
    {
        _url = url;
        _timer = new System.Timers.Timer(IntervalM * 60 * 1000);
        _timer.Elapsed += async (sender, e) => await OnTimerElapsedAsync();
    }

    #endregion

    #region Public Methods

    public void Start()
    {
        if (!_timer.Enabled)
        {
            _timer.Start();
        }
    }

    public void Stop()
    {
        if (_timer.Enabled)
        {
            _timer.Stop();
        }
    }

    #endregion

    #region Private Methods

    private async Task OnTimerElapsedAsync()
    {
        var data = await NetUtils.GetAsync(_url);
        if (string.IsNullOrEmpty(data))
        {
            // Failed to fetch active events
            Console.WriteLine($"Failed to fetch active Pokemon Go events manifest.");
            return;
        }

        // Prevents false report upon first check
        if (string.IsNullOrEmpty(_previousData))
        {
            // First time fetching, set _previousData
            _previousData = data;
            return;
        }

        // Check if previous data downloaded is the same as current data.
        if (Equals(_previousData, data))
        {
            // If so, skip
            return;
        }

        // Set previous data to current data since it has changed.
        _previousData = data;

        try
        {
            var events = data.FromJson<List<ActiveEvent>>();
            if (events == null)
            {
                // Failed to deserialize fetched active events
                Console.WriteLine($"Failed to deserialize fetched active Pokemon Go events manifest.");
                return;
            }

            OnChanged(events);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex}");
        }
    }

    #endregion

    public sealed class EventChangedEventArgs : EventArgs
    {
        public IEnumerable<IActiveEvent> Events { get; }

        public EventChangedEventArgs(IEnumerable<IActiveEvent> events)
        {
            Events = events;
        }
    }
}