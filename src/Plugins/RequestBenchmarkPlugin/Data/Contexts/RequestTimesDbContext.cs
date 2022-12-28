namespace RequestBenchmarkPlugin.Data.Contexts;

using Microsoft.EntityFrameworkCore;

using Data.Entities;

public class RequestTimesDbContext : DbContext
{
    public DbSet<RequestTime> RequestTimes => Set<RequestTime>();

    public RequestTimesDbContext(DbContextOptions<RequestTimesDbContext> options)
        : base(options)
    {
        base.ChangeTracker.AutoDetectChangesEnabled = false;
        base.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }
}