namespace ChuckDeviceController.JobControllers
{
    using System.Threading.Tasks;

    public interface IJobController
    {
        public string Name { get; }

        public ushort MinimumLevel { get; }

        public ushort MaximumLevel { get; }

        public Task<ITask> GetTask(string uuid, string accountUsername, bool startup);

        public Task<string> GetStatus();

        public void Stop();

        public void Reload();
    }
}