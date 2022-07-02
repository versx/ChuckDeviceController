namespace ChuckDeviceConfigurator.Services.Tasks
{
	public class BootstrapTask : BaseJobTask
	{
		public BootstrapTask()
		{
			Action = DeviceActionType.ScanRaid;
		}
	}
}