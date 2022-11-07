namespace ChuckDeviceController.Services
{
    using ChuckDeviceController.Data.Entities;

    public interface IDataConsumerService
    {
        //Task ConsumeDataAsync(CancellationToken stoppingToken);

        // TODO: Use stateless/generic processing
        //Task AddEntityAsync(BulkOperation<TEntity> options, TEntity entity);

        //Task AddEntitiesAsync(BulkOperation<TEntity> options, IEnumerable<TEntity> entities);

        Task AddPokemonAsync(string query, Pokemon entity);

        Task AddPokestopAsync(string query, Pokestop entity);

        Task AddGymAsync(string query, Gym entity);

        Task AddGymDefenderAsync(string query, GymDefender entity);

        Task AddGymTrainerAsync(string query, GymTrainer entity);

        Task AddIncidentAsync(string query, Incident entity);

        Task AddIncidentsAsync(string query, IEnumerable<Incident> entities);

        Task AddSpawnpointAsync(string query, Spawnpoint entity);

        Task AddWeatherAsync(string query, Weather entity);

        Task AddCellAsync(string query, Cell entity);

        Task AddCellsAsync(string query, IEnumerable<Cell> entities);

        Task AddAccountAsync(string query, Account entity);
    }
}