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

    // TODO: Map data entities

    #endregion
}
