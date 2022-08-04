namespace TestPlugin.JobControllers
{
    using ChuckDeviceController.Plugins;

    public class TestInstanceController : IJobController
    {
        #region Properties

        public string Name { get; }

        public IReadOnlyList<ICoordinate> Coordinates { get; }

        public ushort MinimumLevel { get; }

        public ushort MaximumLevel { get; }

        //public string GroupName { get; }

        //public bool IsEvent { get; }

        #endregion

        #region Constructor

        public TestInstanceController(string name, ushort minLevel, ushort maxLevel, List<ICoordinate> coords)
        {
            Name = name;
            MinimumLevel = minLevel;
            MaximumLevel = maxLevel;
            Coordinates = coords;
        }

        #endregion

        #region Public Methods

        public async Task<ITask> GetTaskAsync(TaskOptions options)
        {
            await Task.CompletedTask;
            return null;
        }

        public async Task<string> GetStatusAsync()
        {
            await Task.CompletedTask;
            return null;
        }

        public Task ReloadAsync()
        {
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            return Task.CompletedTask;
        }

        #endregion
    }
}