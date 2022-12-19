namespace ChuckDeviceController.JobControllers.Tasks;

using ChuckDeviceController.Common;
using ChuckDeviceController.Common.Tasks;

public class SwitchAccountTask : BaseJobTask
{
	public SwitchAccountTask()
	{
		Action = DeviceActionType.SwitchAccount;
	}
}