namespace ChuckDeviceController.HostedServices
{
    public class ClearFortsHostedService : HostApplicationLifetimeEventsHostedService
    {
        private readonly ILogger<ClearFortsHostedService> _logger;

        public ClearFortsHostedService(ILogger<ClearFortsHostedService> logger)
            : base(null)
        {
            _logger = logger;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            await base.StartAsync(cancellationToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await base.StopAsync(cancellationToken);
        }

        public override void OnStarted()
        {
            base.OnStarted();
        }
    }

    public class HostApplicationLifetimeEventsHostedService : IHostedService
    {
        private readonly IHostApplicationLifetime _hostApplicationLifetime;

        public HostApplicationLifetimeEventsHostedService(
            IHostApplicationLifetime hostApplicationLifetime)
            => _hostApplicationLifetime = hostApplicationLifetime;

        public virtual Task StartAsync(CancellationToken cancellationToken)
        {
            _hostApplicationLifetime.ApplicationStarted.Register(OnStarted);
            _hostApplicationLifetime.ApplicationStopping.Register(OnStopping);
            _hostApplicationLifetime.ApplicationStopped.Register(OnStopped);

            return Task.CompletedTask;
        }

        public virtual Task StopAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;

        public virtual void OnStarted()
        {
        }

        public virtual void OnStopping()
        {
        }

        public virtual void OnStopped()
        {
        }
    }
}