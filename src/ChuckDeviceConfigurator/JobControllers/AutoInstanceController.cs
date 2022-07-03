namespace ChuckDeviceConfigurator.JobControllers
{
    using System.Threading.Tasks;

    using ChuckDeviceConfigurator.Services.Jobs;
    using ChuckDeviceConfigurator.Services.Tasks;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Geometry.Models;

    public class AutoInstanceController : IJobController
    {
        #region Properties

        public string Name { get; }

        public ushort MinimumLevel { get; }

        public ushort MaximumLevel { get; }

        public string GroupName  { get; }

        public bool IsEvent  { get; }

        #endregion

        #region Constructor

        public AutoInstanceController(Instance instance, List<List<Coordinate>> coords)
        {
            Name = instance.Name;
            MinimumLevel = instance.MinimumLevel;
            MaximumLevel = instance.MaximumLevel;
            GroupName = instance.Data?.AccountGroup;
            IsEvent = instance.Data?.IsEvent ?? false;
        }

        #endregion

        public Task<string> GetStatusAsync()
        {
            throw new NotImplementedException();
        }

        public Task<ITask> GetTaskAsync(string uuid, string? accountUsername = null, bool isStartup = false)
        {
            throw new NotImplementedException();
        }

        public void Reload()
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }
    }
}