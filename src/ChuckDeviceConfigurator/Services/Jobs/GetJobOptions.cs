namespace ChuckDeviceConfigurator.Services.Jobs
{
    using ChuckDeviceController.Data.Entities;

    public class GetTaskOptions
    {
        public string Uuid { get; set; }

        public string? AccountUsername { get; set; } = null;

        public Account? Account { get; set; } = null;
    }
}