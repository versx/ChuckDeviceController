namespace ChuckDeviceController.Data.Utilities
{
    using System.Linq.Expressions;

    using Z.BulkOperations;

    using ChuckDeviceController.Common.Data.Contracts;

    public static class MySqlBulkUtils
    {
        public static BulkOperation<TEntity> GetBulkOptions<TEntity>(
            Expression<Func<TEntity, object>>? onMergeUpdateInputExpression = null,
            Expression<Func<TEntity, object>>? ignoreOnMergeUpdateExpression = null,
            bool allowDuplicateKeys = true,
            bool useTableLock = true
        ) where TEntity : class, IBaseEntity
        {
            var options = new BulkOperation<TEntity>
            {
                AllowDuplicateKeys = allowDuplicateKeys,
                UseTableLock = useTableLock,
                OnMergeUpdateInputExpression = onMergeUpdateInputExpression,
                IgnoreOnMergeUpdateExpression = ignoreOnMergeUpdateExpression,
            };
            return options;
        }
    }
}