﻿namespace Chuck.Data.Repositories
{
    using System;

    using Microsoft.EntityFrameworkCore;

    using Chuck.Data.Contexts;
    using Chuck.Data.Entities;
    using Chuck.Extensions;

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
                ConsoleExt.WriteInfo($"[RawSql] Result -> OK");
                return true;
            }
            catch (Exception) // ex)
            {
                //this log not needed because log says warn.
                //ConsoleExt.WriteError($"[RawSql] Result: {ex}, SQL: {sql}");
                return false;
            }
        }
    }
}
