namespace ChuckDeviceController.Data.Repositories
{
    using Microsoft.EntityFrameworkCore.Storage;

    using ChuckDeviceController.Data.Entities;

    public interface IUnitOfWork : IDisposable
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

        IDbContextTransaction? Transaction { get; }

        #endregion

        #region Methods

        IDbContextTransaction BeginTransaction();

        Task<IDbContextTransaction> BeginTransactionAsync();

        bool Commit();

        Task<bool> CommitAsync();

        void Rollback();

        Task RollbackAsync();

        #endregion
    }
}