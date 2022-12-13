namespace ChuckDeviceController.JobControllers.Tasks
{
    using ChuckDeviceController.Common;
	using ChuckDeviceController.Common.Tasks;

	public class BootstrapTask : BaseJobTask
	{
		public BootstrapTask()
		{
			Action = DeviceActionType.ScanRaid;
		}
	}
}