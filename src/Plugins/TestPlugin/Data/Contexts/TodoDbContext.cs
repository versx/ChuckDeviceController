namespace TestPlugin.Data.Contexts
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;

    using Entities;
    using ChuckDeviceController.Plugins.Services;

    [
        PluginService(
            ServiceType = typeof(DbContext),
            ProxyType = typeof(TodoDbContext),
            Provider = PluginServiceProvider.Plugin,
            Lifetime = ServiceLifetime.Scoped)
    ]
    public class TodoDbContext : DbContext
    {
        public DbSet<Todo> Todos => Set<Todo>();

        public TodoDbContext(DbContextOptions<TodoDbContext> options)
            : base(options)
        {
        }
    }
}