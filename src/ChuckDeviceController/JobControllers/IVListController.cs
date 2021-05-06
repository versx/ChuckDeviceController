namespace ChuckDeviceController.JobControllers
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Logging;

    using Chuck.Data.Entities;
    using Chuck.Data.Factories;
    using Chuck.Data.Repositories;

    public class IVListController
    {
        #region Variables

        //private readonly ILogger<IVListController> _logger;

        private readonly IDictionary<string, IVList> _ivLists;
        private readonly IVListRepository _ivListRepository;

        private readonly object _ivListsLock = new object();

        #endregion

        #region Singleton

        private static IVListController _instance;
        public static IVListController Instance =>
            _instance ??= new IVListController();

        #endregion

        public IVListController()
        {
            _ivLists = new Dictionary<string, IVList>();
            _ivListRepository = new IVListRepository(DbContextFactory.CreateDeviceControllerContext(Startup.DbConnectionString));
            //_logger = new Logger<IVListController>(LoggerFactory.Create(x => x.AddConsole()));
            Reload().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public async Task Reload()
        {
            var ivLists = await _ivListRepository.GetAllAsync().ConfigureAwait(false);
            lock (_ivListsLock)
            {
                _ivLists.Clear();
                foreach (var ivList in ivLists)
                {
                    if (!_ivLists.ContainsKey(ivList.Name))
                    {
                        _ivLists.Add(ivList.Name, ivList);
                    }
                }
            }
        }

        public IVList GetIVList(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            if (!_ivLists.ContainsKey(name))
            {
                return null;
            }
            return _ivLists[name];
        }
    }
}