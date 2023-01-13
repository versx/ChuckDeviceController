namespace TodoPlugin.Data.Contexts;

using Microsoft.EntityFrameworkCore;

using Data.Entities;

public class TodoDbContext : DbContext
{
    public DbSet<Todo> Todos => Set<Todo>();

    public TodoDbContext(DbContextOptions<TodoDbContext> options)
        : base(options)
    {
        base.Database.EnsureCreated();
        base.ChangeTracker.AutoDetectChangesEnabled = false;
        base.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }
}