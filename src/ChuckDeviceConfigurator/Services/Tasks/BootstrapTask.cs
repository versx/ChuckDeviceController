namespace ChuckDeviceConfigurator.Services.Tasks
{
    using ChuckDeviceController.Common;

    public class BootstrapTask : BaseJobTask
	{
		public BootstrapTask()
		{
			Action = DeviceActionType.ScanRaid;
		}
	}
}