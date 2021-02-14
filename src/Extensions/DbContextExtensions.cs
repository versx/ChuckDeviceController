namespace ChuckDeviceController.Extensions
{
    using System;
    using System.Collections.Generic;

    using Microsoft.EntityFrameworkCore;

    using ChuckDeviceController.Data.Interfaces;

    public static class DbContextExtensions
    {
        public static void AddOrUpdate(this DbContext ctx, object entity)
        {
            var entry = ctx.Entry(entity);
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
            foreach (var entity in entities)
            {
                AddOrUpdate(ctx, entity);
            }
        }
    }
}