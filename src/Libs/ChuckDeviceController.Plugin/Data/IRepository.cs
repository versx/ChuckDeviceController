namespace ChuckDeviceController.Plugin;

/// <summary>
/// Repository contract for specific database entity types.
/// </summary>
/// <typeparam name="TEntity">Database entity contract type.</typeparam>
/// <typeparam name="TId">Database entity primary key type.</typeparam>
public interface IRepository<TEntity, TId>
{
    /// <summary>
    /// Gets a list of database entities.
    /// </summary>
    /// <returns>Returns a list of database entities.</returns>
    Task<IReadOnlyList<TEntity>> GetListAsync();

    /// <summary>
    /// Gets a database entity by primary key.
    /// </summary>
    /// <param name="id">Primary key of the database entity.</param>
    /// <returns>Returns a database entity.</returns>
    Task<TEntity> GetByIdAsync(TId id);
}