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
                if (string.IsNullOrEmpty(result.ToString()) || result != 0)
                {
                    return false;
                }
                ConsoleColor org = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("info: RawSql Result -> OK");
                Console.ForegroundColor = org;
                return true;
            }
            catch (Exception ex)
            {
                ConsoleColor org = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error RawSql Result: {ex}, Sql: {sql}");
                Console.ForegroundColor = org;
                return false;
            }
        }
    }
}