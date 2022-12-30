namespace ChuckDeviceController.Data.Repositories;

using Microsoft.EntityFrameworkCore.Storage;

using ChuckDeviceController.Data.Entities;
using ChuckDeviceController.Data.Repositories.EntityFrameworkCore;

public interface IUnitOfWork : IBaseUnitOfWork<IDbContextTransaction>
{
    #region Properties

    IGenericEntityRepository<Account> Accounts { get; }

    IGenericEntityRepository<ApiKey> ApiKeys { get; }

    IGenericEntityRepository<Assignment> Assignments { get; }

    IGenericEntityRepository<AssignmentGroup> AssignmentGroups { get; }

    IGenericEntityRepository<Device> Devices { get; }

    IGenericEntityRepository<DeviceGroup> DeviceGroups { get; }

    IGenericEntityRepository<Geofence> Geofences { get; }

    IGenericEntityRepository<Instance> Instances { get; }

    IGenericEntityRepository<IvList> IvLists { get; }

    IGenericEntityRepository<Webhook> Webhooks { get; }

    #endregion
}