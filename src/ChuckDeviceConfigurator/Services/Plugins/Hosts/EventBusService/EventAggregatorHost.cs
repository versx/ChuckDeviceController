namespace ChuckDeviceConfigurator.Services.Plugins.Hosts.EventBusService
{
    using ChuckDeviceController.Plugin.EventBus;

    public class EventAggregatorHost : IEventAggregatorHost
    {
        #region Variables

        private static readonly Logger<IEventAggregatorHost> _logger =
            new(LoggerFactory.Create(x => x.AddConsole()));
        private readonly List<IEvent> _events;
        private readonly List<IEvent> _eventsRelayed;
        private readonly List<IObserver<IEvent>> _observers;
        private readonly Dictionary<Type, List<IObserver<IEvent>>> _typedObservers;

        #endregion

        #region Properties

        public IEnumerable<IObserver<IEvent>> Observers => _observers;

        public IReadOnlyDictionary<Type, IReadOnlyList<IObserver<IEvent>>> TypedObservers =>
            (IReadOnlyDictionary<Type, IReadOnlyList<IObserver<IEvent>>>)_typedObservers;

        #endregion

        #region Constructor

        public EventAggregatorHost()
        {
            _events = new List<IEvent>();
            _eventsRelayed = new List<IEvent>();
            _observers = new List<IObserver<IEvent>>();
            _typedObservers = new Dictionary<Type, List<IObserver<IEvent>>>();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Publish an event to subscribers.
        /// </summary>
        /// <param name="event">The event to publish.</param>
        public void Publish(IEvent @event)
        {
            _events.Add(@event);

            foreach (var observer in _observers)
            {
                // Provide the observer with new data.
                var result = ExecutePublish(observer, @event);
                if (result != EventExecutionResult.Executed)
                {
                    // Error occurred
                    _logger.LogError($"Failed to emit event bus event '{@event.GetType().FullName}' with result '{result}' to observer.");
                }
            }

            // Check if provided event has a cached strongly typed observer
            if (!_typedObservers.TryGetValue(@event.GetType(), out var typedObservers))
            {
                return;
            }

            foreach (var observer in typedObservers)
            {
                // Provide the typed observer with new data.
                var result = ExecutePublish(observer, @event);
                if (result != EventExecutionResult.Executed)
                {
                    // Error occurred
                    _logger.LogError($"Failed to emit event bus event '{@event.GetType().FullName}' with result '{result}' to typed observer");
                }
            }
        }

        /// <summary>
        /// Subscribe to all events.
        /// </summary>
        /// <param name="observer">The subscriber.</param>
        /// <returns>An unsubscriber to allow unsubscribing from events.</returns>
        public IDisposable Subscribe(IObserver<IEvent> observer) =>
            SubscribeToAllEvents(observer);

        /// <summary>
        /// Subscribe to all events.
        /// </summary>
        /// <param name="observer">The subscriber.</param>
        /// <returns>An unsubscriber to allow unsubscribing from events.</returns>
        public IDisposable Subscribe(ICustomObserver<IEvent> observer) =>
            SubscribeToAllEvents(observer);

        /// <summary>
        /// Subscribe to a specific event type.
        /// </summary>
        /// <typeparam name="T">The event type to subscribe to.</typeparam>
        /// <param name="newObserver">The subscriber.</param>
        /// <returns>An unsubscriber to allow unsubscribing from events.</returns>
        public IDisposable Subscribe<T>(ICustomObserver<T> newObserver)
            where T : IEvent
        {
            if (!_typedObservers.TryGetValue(typeof(T), out var observers))
            {
                observers = new List<IObserver<IEvent>>();
                _typedObservers[typeof(T)] = observers;
            }

            var events = _events.Where(evt => evt is T);
            return SubscribeAndSendEvents(observers, newObserver, events);
        }

        #endregion

        #region Private Methods

        private IDisposable SubscribeToAllEvents(IObserver<IEvent> newObserver) =>
            SubscribeAndSendEvents(_observers, newObserver, _events);

        private IDisposable SubscribeAndSendEvents(
            List<IObserver<IEvent>> currentObservers,
            IObserver<IEvent> newObserver,
            IEnumerable<IEvent> events)
        {
            if (!currentObservers.Contains(newObserver))
            {
                currentObservers.Add(newObserver);

                // Provide observer with existing data.
                foreach (var @event in events)
                {
                    var result = ExecutePublish(newObserver, @event);
                    if (result != EventExecutionResult.Executed)
                    {
                        // Error occurred
                        _logger.LogError($"Failed to publish event '{@event.GetType().FullName}' to event bus with result '{result}'");
                    }

                    // TODO: Use lock
                    if (_events.Contains(@event))
                    {
                        _events.Remove(@event);
                    }
                }
            }

            return new Unsubscriber<IEvent>(currentObservers, newObserver);
        }

        private EventExecutionResult ExecutePublish(IObserver<IEvent> observer, IEvent @event)
        {
            EventExecutionResult result;
            try
            {
                observer.OnNext(@event);
                _eventsRelayed.Add(@event);
                result = EventExecutionResult.Executed;
            }
            catch (Exception ex)
            {
                _logger.LogError($"ExecutePublish: {ex}");
                result = EventExecutionResult.UnhandledException;
            }
            return result;
        }

        #endregion
    }
}