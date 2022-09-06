namespace ChuckDeviceConfigurator.Services.IvLists
{
	using Microsoft.EntityFrameworkCore;

	using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;

    public class IvListControllerService : IIvListControllerService
    {
		#region Variables

		//private readonly ILogger<IIvListControllerService> _logger;
		private readonly IDbContextFactory<ControllerDbContext> _factory;

		private readonly object _ivListsLock = new();
		private List<IvList> _ivLists;

		#endregion

		#region Constructor

		public IvListControllerService(
			//ILogger<IIvListControllerService> logger,
			IDbContextFactory<ControllerDbContext> factory)
		{
			//_logger = logger;
			_factory = factory;
			_ivLists = new();

			Reload();
		}

		#endregion

		#region Public Methods

		public void Reload()
		{
			lock (_ivListsLock)
			{
				_ivLists = GetIvLists();
			}
		}

		public void Add(IvList ivList)
		{
			lock (_ivListsLock)
			{
				if (_ivLists.Contains(ivList))
				{
					// Already exists
					return;
				}
				_ivLists.Add(ivList);
			}
		}

		public void Edit(IvList newIvList, string oldIvListName)
		{
			Delete(oldIvListName);
			Add(newIvList);
		}

		public void Delete(string name)
		{
			lock (_ivListsLock)
			{
				_ivLists = _ivLists.Where(x => x.Name != name)
								   .ToList();
			}
		}

		public IvList GetByName(string name)
		{
			IvList? ivList = null;
			lock (_ivListsLock)
			{
				ivList = _ivLists.Find(x => x.Name == name);
			}
			return ivList;
		}

		public IReadOnlyList<IvList> GetByNames(IReadOnlyList<string> names)
		{
			return names.Select(name => GetByName(name))
						.ToList();
		}

		#endregion

		#region Private Methods

		private List<IvList> GetIvLists()
		{
			using (var context = _factory.CreateDbContext())
			{
				var ivLists = context.IvLists.ToList();
				return ivLists;
			}
		}

		#endregion
	}
}