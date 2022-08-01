namespace ChuckDeviceConfigurator.Services.Webhooks
{
	using Microsoft.EntityFrameworkCore;

	using ChuckDeviceConfigurator.Extensions;
	using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;

    public class WebhookControllerService : IWebhookControllerService
	{
		#region Variables

		//private readonly ILogger<IWebhookControllerService> _logger;
		private readonly IDbContextFactory<DeviceControllerContext> _factory;

		private readonly object _webhooksLock = new();
		private List<Webhook> _webhooks;

		#endregion

		#region Constructor

		public WebhookControllerService(
			//ILogger<IWebhookControllerService> logger,
			IDbContextFactory<DeviceControllerContext> factory)
		{
			//_logger = logger;
			_factory = factory;
			_webhooks = new();

			Reload();
		}

		#endregion

		#region Public Methods

		public void Reload()
		{
			lock (_webhooksLock)
			{
				_webhooks = (List<Webhook>)GetAll();
			}
		}

		public void Add(Webhook webhook)
		{
			lock (_webhooksLock)
			{
				if (_webhooks.Contains(webhook))
				{
					// Already exists
					return;
				}
				_webhooks.Add(webhook);
			}
		}

		public void Edit(Webhook newWebhook, string oldWebhookName)
		{
			Delete(oldWebhookName);
			Add(newWebhook);
		}

		public void Delete(string name)
		{
			lock (_webhooksLock)
			{
				_webhooks = _webhooks.Where(x => x.Name != name)
								     .ToList();
			}
		}

		public Webhook GetByName(string name)
		{
			Webhook? webhook = null;
			lock (_webhooksLock)
			{
				webhook = _webhooks.Find(x => x.Name == name);
			}
			return webhook;
		}

		public IReadOnlyList<Webhook> GetByNames(IReadOnlyList<string> names)
		{
			return names.Select(name => GetByName(name))
						.ToList();
		}

		public IReadOnlyList<Webhook> GetAll(bool includeGeofenceMultiPolygons = false)
		{
			using (var context = _factory.CreateDbContext())
			{
				var webhooks = context.Webhooks.ToList();
				if (includeGeofenceMultiPolygons)
                {
					var geofences = context.Geofences.ToList();
					webhooks.ForEach(webhook =>
					{
						foreach (var webhookGeofence in webhook.Geofences)
                        {
							var geofence = geofences.FirstOrDefault(g => g.Name == webhookGeofence);
							if (geofence == null)
								continue;

							var (multiPolygons, _) = geofence.ConvertToMultiPolygons();
							webhook.GeofenceMultiPolygons = multiPolygons;
                        }
					});
                }
				return webhooks;
			}
		}

		#endregion
	}
}
