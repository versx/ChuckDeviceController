namespace ChuckDeviceController.JobControllers.Tasks
{
    using ChuckDeviceController.Common;

    public class SwitchAccountTask : BaseJobTask
	{
		public SwitchAccountTask()
        {
			Action = DeviceActionType.SwitchAccount;
        }
	}
}