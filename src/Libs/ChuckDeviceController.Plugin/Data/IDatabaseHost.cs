namespace ChuckDeviceController.Plugin;

using System.Linq.Expressions;

using ChuckDeviceController.Data.Common;

/// <summary>
/// Plugin host handler contract used to interact with the database entities.
/// </summary>
public interface IDatabaseHost
{
    //IRepository<IAccount, string> Accounts { get; }
    //IRepository<IPokestop, string> Pokestops { get; }
    //IRepository<IDevice, string> Devices { get; }

    /// <summary>
    /// Gets a list of database entities.
    /// </summary>
    /// <typeparam name="TEntity">Database entity contract type.</typeparam>
    /// <returns>Returns the list of database entities.</returns>
    Task<IReadOnlyList<TEntity>> FindAllAsync<TEntity>()
        where TEntity : class;

    /// <summary>
    /// Gets a database entity by primary key.
    /// </summary>
    /// <typeparam name="TEntity">Database entity contract type.</typeparam>
    /// <typeparam name="TKey">Database entity primary key type.</typeparam>
    /// <param name="id">Primary key of the database entity.</param>
    /// <returns>Returns the database entity.</returns>
    Task<TEntity?> FindAsync<TEntity, TKey>(TKey id)
        where TKey : notnull
        where TEntity : class;

    /// <summary>
    /// Gets a list of database entities matching the specified criteria.
    /// </summary>
    /// <typeparam name="TKey">Entity property type when sorting.</typeparam>
    /// <typeparam name="TEntity">Database entity contract type.</typeparam>
    /// <param name="predicate">Predicate used to determine if a database entity matches.</param>
    /// <param name="order">Sort order expression. (Optional)</param>
    /// <param name="sortDirection">Sort ordering direction.</param>
    /// <param name="limit">Limit the returned number of results.</param>
    /// <returns>Returns the list of database entities.</returns>
    Task<IReadOnlyList<TEntity>> FindAsync<TEntity, TKey>(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, TKey>>? order = null,
        SortOrderDirection sortDirection = SortOrderDirection.Asc,
        int limit = 1000)
        where TEntity : class;
}