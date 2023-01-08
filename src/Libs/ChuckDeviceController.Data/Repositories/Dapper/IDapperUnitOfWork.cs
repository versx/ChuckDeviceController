namespace ChuckDeviceController.Data.Repositories.Dapper;

using MySqlConnector;

using ChuckDeviceController.Data.Entities;

public interface IDapperUnitOfWork : IBaseUnitOfWork<MySqlTransaction>
{
    #region Properties

    IDapperGenericRepository<string, Account> Accounts { get; }

    IDapperGenericRepository<uint, ApiKey> ApiKeys { get; }

    IDapperGenericRepository<uint, Assignment> Assignments { get; }

    IDapperGenericRepository<string, AssignmentGroup> AssignmentGroups { get; }

    IDapperGenericRepository<string, Device> Devices { get; }

    IDapperGenericRepository<string, DeviceGroup> DeviceGroups { get; }

    IDapperGenericRepository<string, Geofence> Geofences { get; }

    IDapperGenericRepository<string, Instance> Instances { get; }

    IDapperGenericRepository<string, IvList> IvLists { get; }

    IDapperGenericRepository<string, Webhook> Webhooks { get; }


    IDapperGenericRepository<ulong, Cell> Cells { get; }

    IDapperGenericRepository<string, Gym> Gyms { get; }

    IDapperGenericRepository<ulong, GymDefender> GymDefenders { get; }

    IDapperGenericRepository<string, GymTrainer> GymTrainers { get; }

    IDapperGenericRepository<string, Incident> Incidents { get; }

    IDapperGenericRepository<string, Pokemon> Pokemon { get; }

    IDapperGenericRepository<string, Pokestop> Pokestops { get; }

    IDapperGenericRepository<ulong, Spawnpoint> Spawnpoints { get; }

    IDapperGenericRepository<long, Weather> Weather { get; }

    #endregion
}
