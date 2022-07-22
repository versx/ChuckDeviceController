namespace ChuckDeviceConfigurator.Services.IvLists
{
	using ChuckDeviceController.Data.Entities;

	/// <summary>
	/// Caches all configured IV lists to reduce database loads.
	/// </summary>
	public interface IIvListControllerService : IControllerService<IvList, string>
	{
	}
}