namespace TodoPlugin.Data.Contexts
{
    using Microsoft.EntityFrameworkCore;

    using Entities;

    public class TodoDbContext : DbContext
    {
        public DbSet<Todo> Todos => Set<Todo>();

        public TodoDbContext(DbContextOptions<TodoDbContext> options)
            : base(options)
        {
        }
    }
}