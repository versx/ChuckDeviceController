namespace ChuckDeviceController.Data.Repositories
{
    using System;

    using Microsoft.EntityFrameworkCore;

    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;

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
                var result = _dbContext?.Database.ExecuteSqlRaw(sql);
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