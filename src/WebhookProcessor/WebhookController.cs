namespace WebhookProcessor
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    using Chuck.Infrastructure.Data.Entities;
    using Chuck.Infrastructure.Extensions;
    using Chuck.Infrastructure.Queues;
    using Chuck.Infrastructure.Utilities;

    // TODO: Filter webhooks by area
    // TODO: Filter webhooks by blacklists

    public class WebhookController
    {
        #region Variables

        private readonly List<dynamic> _sentEvents;
        private readonly IQueue<dynamic> _queue;
        private readonly object _webhooksLock = new object();
        private readonly object _lock = new object();

        private Thread _thread;
        //private readonly System.Timers.Timer _timer;

        #endregion

        #region Singleton

        private static WebhookController _instance;
        public static WebhookController Instance =>
            _instance ??= new WebhookController();

        #endregion

        #region Properties

        public bool IsRunning { get; private set; }

        public ushort SleepIntervalS { get; set; }

        public IReadOnlyList<Webhook> Webhooks { get; set; }

        #endregion

        #region Constructor

        public WebhookController()
        {
            _sentEvents = new List<dynamic>();
            _queue = new WebhookQueue<dynamic>();
            /*
            _timer = new System.Timers.Timer
            {
                Interval = 5 * 1000, // TODO: Configurable
            };
            _timer.Elapsed += (sender, e) => OnTimeElapsed();
            */
            SleepIntervalS = 5;
        }

        #endregion

        #region Public Methods

        public void Start()
        {
            //_timer.Start();
            IsRunning = true;
            if (_thread == null)
            {
                _thread = new Thread(_ => OnTimeElapsed())
                {
                    IsBackground = true,
                };
            }
            _thread.Start();
        }

        public void Stop()
        {
            //_timer.Stop();
            IsRunning = false;
            if (_thread == null)
                return;

            _thread.Interrupt();
            if (!_thread.Join(2000))
            {
                ConsoleExt.WriteError($"[WebhookController] Failed to abort webhook thread");
            }
            _thread = null;
        }

        public void Add(dynamic payload)
        {
            if (Webhooks?.Count == 0)
                return;

            if (!_sentEvents.Contains(payload))
            {
                lock (_lock)
                {
                    _queue.Enqueue(payload);
                }
            }
        }

        public void SetWebhooks(List<Webhook> webhooks)
        {
            Webhooks = webhooks;
        }

        #endregion

        #region Private Methods

        private void OnTimeElapsed()
        {
            while (IsRunning)
            {
                var events = new List<dynamic>();
                if (_queue.Count > 0)
                {
                    lock (_lock)
                    {
                        while (_queue.Count > 0)
                        {
                            var payload = _queue.Dequeue();
                            //_sentEvents.Add(payload);
                            events.Add(payload);
                            Thread.Sleep(1);
                        }
                    }
                }

                if (events.Count == 0)
                {
                    Thread.Sleep(SleepIntervalS * 1000);
                    continue;
                }

                foreach (var webhook in Webhooks)
                {
                    SendEvents(webhook.Url, events);
                }
                _sentEvents.AddRange(events);

                Thread.Sleep(SleepIntervalS * 1000);
            }
        }

        private static bool SendEvents(string url, List<dynamic> events, ushort retryCount = 0)
        {
            if (events == null || events.Count == 0)
                return false;

            NetUtil.SendWebhook(url, events.ToJson(), retryCount);
            ConsoleExt.WriteInfo($"[WebhookController] Sent {events.Count} webhook events to {url}");
            return true;
        }

        #endregion
    }
}