namespace ChuckDeviceController.Extensions
{
    using Microsoft.EntityFrameworkCore;
    using System;
    using System.Collections.Generic;

    public static class DbContextExtensions
    {
        public static void AddOrUpdate(this DbContext ctx, object entity)
        {
            Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry = ctx.Entry(entity);
            switch (entry.State)
            {
                case EntityState.Detached:
                    ctx.Add(entity);
                    break;
                case EntityState.Modified:
                    ctx.Update(entity);
                    break;
                case EntityState.Added:
                    ctx.Add(entity);
                    break;
                case EntityState.Unchanged:
                    //item already in db no need to do anything  
                    break;
                default:
#pragma warning disable CA2208 // Instancier les exceptions d'argument correctement
                    throw new ArgumentOutOfRangeException();
#pragma warning restore CA2208 // Instancier les exceptions d'argument correctement
            }
        }

        public static void AddOrUpdateRange(this DbContext ctx, List<object> entities)
        {
            foreach (object entity in entities)
            {
                AddOrUpdate(ctx, entity);
            }
        }
    }
}