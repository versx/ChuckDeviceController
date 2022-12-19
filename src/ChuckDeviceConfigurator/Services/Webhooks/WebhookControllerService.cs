namespace ChuckDeviceConfigurator.Services.Webhooks;

using Microsoft.EntityFrameworkCore;

using ChuckDeviceController.Collections;
using ChuckDeviceController.Data.Contexts;
using ChuckDeviceController.Data.Entities;
using ChuckDeviceController.Data.Extensions;

public class WebhookControllerService : IWebhookControllerService
{
	#region Variables

	private readonly ILogger<IWebhookControllerService> _logger;
	private readonly IDbContextFactory<ControllerDbContext> _factory;
	private SafeCollection<Webhook> _webhooks;

	#endregion

	#region Constructor

	public WebhookControllerService(
		ILogger<IWebhookControllerService> logger,
		IDbContextFactory<ControllerDbContext> factory)
	{
		_logger = logger;
		_factory = factory;
		_webhooks = new();

		Reload();
	}

	#endregion

	#region Public Methods

	public void Reload()
	{
		var webhooks = GetAll();
		_webhooks = new(webhooks);
	}

	public void Add(Webhook webhook)
	{
		if (_webhooks.Contains(webhook))
		{
			// Already exists
			return;
		}
		if (!_webhooks.TryAdd(webhook))
		{
			_logger.LogError($"Failed to add webhook with name '{webhook.Name}'");
		}
	}

	public void Edit(Webhook newWebhook, string oldWebhookName)
	{
		Delete(oldWebhookName);
		Add(newWebhook);
	}

	public void Delete(string name)
	{
		//_webhooks = new(_webhooks.Where(x => x.Name != name).ToList());
		if (!_webhooks.Remove(x => x.Name == name))
		{
			_logger.LogError($"Failed to remove webhook with name '{name}'");
		}
	}

	public Webhook GetByName(string name)
	{
		var webhook = _webhooks.TryGet(x => x.Name == name);
		return webhook;
	}

	public IReadOnlyList<Webhook> GetByNames(IReadOnlyList<string> names)
	{
		var webhooks = names
			.Select(name => GetByName(name))
			.ToList();
		return webhooks;
	}

	public IReadOnlyList<Webhook> GetAll(bool includeGeofenceMultiPolygons = false)
	{
		using var context = _factory.CreateDbContext();

		var webhooks = context.Webhooks.ToList();
		if (!includeGeofenceMultiPolygons)
			return webhooks;

		var geofences = context.Geofences.ToList();
		foreach (var webhook in webhooks)
		{
			var coordinates = geofences
				.Where(g => webhook.Geofences.Contains(g.Name))
				.Select(g => g.ConvertToMultiPolygons())
				.SelectMany(g => g.Item2)
				.ToList();
			webhook.GeofenceMultiPolygons = coordinates;
		}

		return webhooks;
	}

	#endregion
}