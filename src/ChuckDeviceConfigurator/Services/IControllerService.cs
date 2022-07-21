namespace ChuckDeviceConfigurator.Services
{
	using ChuckDeviceController.Data.Entities;

	/// <summary>
	///		Inheritable interface for controller services that
	///		share very similar methods
	/// </summary>
	/// <typeparam name="T">
	///		EnityFramework Core model that inherits <seealso cref="BaseEntity"/>
	/// </typeparam>
	/// <typeparam name="TId">
	///		Primitive database key type for
	///		model, i.e. string, uint, etc
	/// </typeparam>
	public interface IControllerService<T, TId> where T : BaseEntity
	{
		void Reload();

		void Add(T item);

		void Edit(T newItem, TId oldName);

		void Delete(TId name);

		T GetByName(TId name);

		IReadOnlyList<T> GetByNames(IReadOnlyList<TId> names);
	}
}