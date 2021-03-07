namespace Chuck.Infrastructure.JobControllers
{
    using System.Threading.Tasks;

    public interface IJobController
    {
        string Name { get; }

        ushort MinimumLevel { get; }

        ushort MaximumLevel { get; }

        string GroupName { get; }

        bool IsEvent { get; }

        Task<ITask> GetTask(string uuid, string accountUsername, bool startup);

        Task<string> GetStatus();

        void Stop();

        void Reload();
    }
}