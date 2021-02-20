namespace ChuckDeviceController.Data.Repositories
{
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;
    using Microsoft.EntityFrameworkCore;
    using System;

    public class MetadataRepository : EfCoreRepository<Metadata, DeviceControllerContext>
    {
        public MetadataRepository(DeviceControllerContext context)
            : base(context)
        {
        }

        public bool ExecuteSql(string sql)
        {
            try
            {
                int result = _dbContext.Database.ExecuteSqlRaw(sql);
                Console.WriteLine($"[RawSql] Result: {result}");
                return result == 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RawSql] Error: {ex}, Sql: {sql}");
                return false;
            }
        }
    }
}