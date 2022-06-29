namespace ChuckDeviceConfigurator.Models
{
    using ChuckDeviceController.Data.Entities;

    public class ViewInstanceModel
	{
		public List<string> InstanceTypes { get; set; }

		public List<string> Geofences { get; set; }

		public Instance Instance { get; set; }
	}
}