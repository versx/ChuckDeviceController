namespace ChuckDeviceController.Services
{
    using Z.BulkOperations;

    using ChuckDeviceController.Data.Entities;

    public interface IDataConsumerService
    {
        //Task ConsumeDataAsync(CancellationToken stoppingToken);

        // TODO: Use stateless/generic processing
        //Task AddEntityAsync(BulkOperation<TEntity> options, TEntity entity);

        //Task AddEntitiesAsync(BulkOperation<TEntity> options, IEnumerable<TEntity> entities);

        Task AddPokemonAsync(BulkOperation<Pokemon> options, Pokemon entity);

        Task AddPokestopAsync(BulkOperation<Pokestop> options, Pokestop entity);

        Task AddGymAsync(BulkOperation<Gym> options, Gym entity);

        Task AddGymDefenderAsync(BulkOperation<GymDefender> options, GymDefender entity);

        Task AddGymTrainerAsync(BulkOperation<GymTrainer> options, GymTrainer entity);

        Task AddIncidentAsync(BulkOperation<Incident> options, Incident entity);

        Task AddIncidentsAsync(BulkOperation<Incident> options, IEnumerable<Incident> entities);

        Task AddSpawnpointAsync(BulkOperation<Spawnpoint> options, Spawnpoint entity);

        Task AddWeatherAsync(BulkOperation<Weather> options, Weather entity);

        Task AddCellAsync(BulkOperation<Cell> options, Cell entity);

        Task AddCellsAsync(BulkOperation<Cell> options, IEnumerable<Cell> entities);

        Task AddAccountAsync(BulkOperation<Account> options, Account entity);
    }
}