namespace ChuckDeviceConfigurator.Services;

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
	/// <summary>
	/// Loads or reloads the cached entities with latest values.
	/// </summary>
	void Reload();

	/// <summary>
	/// Adds the specified entity to the cache.
	/// </summary>
	/// <param name="item">Item to be added to the cache.</param>
	void Add(T item);

	/// <summary>
	/// Edits the specified entity in the cache.
	/// </summary>
	/// <param name="newItem">
	/// New version of the item to be replaced with in cache.
	/// </param>
	/// <param name="oldName">
	/// Old name of the item to be removed from cache before adding the new version.
	/// </param>
	void Edit(T newItem, TId oldName);

	/// <summary>
	/// Deletes (removes) the specified item by name from the cache.
	/// </summary>
	/// <param name="name">Name (unique key) of the entity.</param>
	void Delete(TId name);

	/// <summary>
	/// Gets the entity by name from the cache.
	/// </summary>
	/// <param name="name">Name of entity to fetch.</param>
	/// <returns>Returns the entity by name.</returns>
	T GetByName(TId name);

	/// <summary>
	/// Gets a list of entities by name from the cache.
	/// </summary>
	/// <param name="names">List of names of entities to fetch.</param>
	/// <returns>Returns the list of entities by name.</returns>
	IReadOnlyList<T> GetByNames(IReadOnlyList<TId> names);
}