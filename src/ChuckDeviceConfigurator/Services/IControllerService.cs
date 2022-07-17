namespace ChuckDeviceConfigurator.Services
{
	public interface IControllerService<T, TId>
	{
		void Reload();

		void Add(T item);

		void Edit(T newItem, TId oldName);

		void Delete(TId name);

		T GetByName(TId name);

		IReadOnlyList<T> GetByNames(IReadOnlyList<TId> names);
	}
}