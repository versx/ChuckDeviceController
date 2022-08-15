namespace TestPlugin.Data.Contexts
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;

    using Entities;
    using ChuckDeviceController.Plugins.Services;

    [PluginService(typeof(DbContext), typeof(TodoDbContext), PluginServiceProvider.Plugin, ServiceLifetime.Scoped)]
    public class TodoDbContext : DbContext
    {
        public TodoDbContext(DbContextOptions<TodoDbContext> options)
            : base(options)
        {
        }

        public DbSet<Todo> Todos => Set<Todo>();
    }
}