namespace ChuckDeviceConfigurator.Services.IvLists;

using ChuckDeviceController.Collections;
using ChuckDeviceController.Data.Entities;
using ChuckDeviceController.Data.Repositories.Dapper;

public class IvListControllerService : IIvListControllerService
{
	#region Variables

	private readonly ILogger<IIvListControllerService> _logger;
	private readonly IDapperUnitOfWork _uow;

	private SafeCollection<IvList> _ivLists;

	#endregion

	#region Constructor

	public IvListControllerService(
		ILogger<IIvListControllerService> logger,
		IDapperUnitOfWork uow)
	{
		_logger = logger;
		_uow = uow;
		_ivLists = new();

		Reload();
	}

	#endregion

	#region Public Methods

	public void Reload()
	{
		var ivLists = GetIvListsAsync().Result;
		_ivLists = new(ivLists);
	}

	public void Add(IvList ivList)
	{
		if (_ivLists.Contains(ivList))
		{
			// Already exists
			return;
		}
		if (!_ivLists.TryAdd(ivList))
		{
			_logger.LogError($"Failed to add IV list with name '{ivList.Name}'");
		}
	}

	public void Edit(IvList newIvList, string oldIvListName)
	{
		Delete(oldIvListName);
		Add(newIvList);
	}

	public void Delete(string name)
	{
		if (!_ivLists.Remove(x => x.Name == name))
		{
			_logger.LogError($"Failed to remove IV list with name '{name}'");
		}
	}
	public IvList GetByName(string name)
	{
		var ivList = _ivLists.TryGet(x => x.Name == name);
		return ivList;
	}

	public IReadOnlyList<IvList> GetByNames(IReadOnlyList<string> names)
	{
		var ivLists = names
			.Select(GetByName)
			.ToList();
		return ivLists;
	}

	#endregion

	#region Private Methods

	private async Task<IEnumerable<IvList>> GetIvListsAsync()
	{
		var ivLists = await _uow.IvLists.FindAllAsync();
		return ivLists;
	}

	#endregion
}