namespace ChuckDeviceController.Data.Contexts;

using Microsoft.EntityFrameworkCore;

using ChuckDeviceController.Data.Entities;

public class PluginDbContext : DbContext
{
    public DbSet<Plugin> Plugins => Set<Plugin>();

    public PluginDbContext(DbContextOptions<PluginDbContext> options)
         : base(options)
    {
        base.Database.EnsureCreated();
        base.ChangeTracker.AutoDetectChangesEnabled = false;
        base.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
    }
}