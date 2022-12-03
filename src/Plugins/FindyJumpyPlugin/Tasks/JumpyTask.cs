namespace FindyJumpyPlugin.Tasks
{
    using ChuckDeviceController.Common.Tasks;

    public class JumpyTask : ITask
    {
        public virtual string Action { get; set; } = null!;

        public virtual double Latitude { get; set; }

        public virtual double Longitude { get; set; }

        public virtual ushort MinimumLevel { get; set; }

        public virtual ushort MaximumLevel { get; set; }

        public override string ToString()
        {
            return $"[Latitude={Latitude}, Longitude={Longitude}]";
        }
    }
}