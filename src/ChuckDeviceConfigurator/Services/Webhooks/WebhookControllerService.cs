namespace ChuckDeviceConfigurator.Services.Webhooks;

using ChuckDeviceController.Collections;
using ChuckDeviceController.Data.Entities;
using ChuckDeviceController.Data.Extensions;
using ChuckDeviceController.Data.Repositories.Dapper;

public class WebhookControllerService : IWebhookControllerService
{
	#region Variables

	private readonly ILogger<IWebhookControllerService> _logger;
	private readonly IDapperUnitOfWork _uow;
	private SafeCollection<Webhook> _webhooks;

	#endregion

	#region Constructor

	public WebhookControllerService(
		ILogger<IWebhookControllerService> logger,
		IDapperUnitOfWork uow)
	{
		_logger = logger;
		_uow = uow;
		_webhooks = new();

		Reload();
	}

	#endregion

	#region Public Methods

	public void Reload()
	{
		var webhooks = GetAllAsync().Result;
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
			.Select(GetByName)
			.ToList();
		return webhooks;
	}

	public async Task<IEnumerable<Webhook>> GetAllAsync(bool includeGeofenceMultiPolygons = false)
	{
		var webhooks = await _uow.Webhooks.FindAllAsync();
		if (!includeGeofenceMultiPolygons)
			return webhooks;

		var geofences = await _uow.Geofences.FindAllAsync();
		foreach (var webhook in webhooks)
		{
			var coordinates = geofences
				.Where(g => webhook.Geofences.Contains(g.Name))
				.Select(g => g.ConvertToMultiPolygons())
				.SelectMany(g => g.Item2)
				.ToList();
			webhook.GeofenceMultiPolygons = coordinates
				.Select(x => x.ToList())
				.ToList();
		}

		return webhooks;
	}

	#endregion
}