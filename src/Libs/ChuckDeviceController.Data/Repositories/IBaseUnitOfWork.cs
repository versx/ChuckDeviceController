namespace ChuckDeviceController.Data.Repositories;

public interface IBaseUnitOfWork<TDbTransaction> : IDisposable
{
    TDbTransaction? Transaction { get; }

    TDbTransaction BeginTransaction();

    Task<TDbTransaction> BeginTransactionAsync(CancellationToken stoppingToken = default);

    bool Commit();

    Task<bool> CommitAsync(CancellationToken stoppingToken = default);

    void Rollback();

    Task RollbackAsync(CancellationToken stoppingToken = default);
}