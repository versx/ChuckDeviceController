namespace TestPlugin.Data.Contexts
{
    using Microsoft.EntityFrameworkCore;

    using Entities;

    public class TodoDbContext : DbContext
    {
        public TodoDbContext(DbContextOptions<TodoDbContext> options)
            : base(options)
        {
        }

        public DbSet<Todo> Todos => Set<Todo>();
    }
}