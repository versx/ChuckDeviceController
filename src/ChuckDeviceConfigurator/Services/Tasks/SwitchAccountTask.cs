namespace ChuckDeviceConfigurator.Services.Tasks
{
	public class SwitchAccountTask : BaseJobTask
	{
		public SwitchAccountTask()
        {
			Action = DeviceActionType.SwitchAccount;
        }
	}
}