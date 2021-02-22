namespace ChuckDeviceController.Data.Repositories
{
    using System;

    using Microsoft.EntityFrameworkCore;

    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Extensions;

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
                ConsoleExt.WriteInfo("[RawSql] Result -> OK");
                return true;
            }
            catch (Exception ex)
            {
                ConsoleExt.WriteError($"[RawSql] Result: {ex}, Sql: {sql}");
                return false;
            }
        }
    }
}