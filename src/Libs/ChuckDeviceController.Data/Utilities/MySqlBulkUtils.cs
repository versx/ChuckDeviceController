namespace ChuckDeviceController.Data.Utilities
{
    using System.Linq.Expressions;

    using Z.BulkOperations;

    using ChuckDeviceController.Data.Entities;

    public static class MySqlBulkUtils
    {
        public static BulkOperation<T> GetBulkOptions<T>(
            Expression<Func<T, object>> onMergeUpdateInputExpression,
            bool allowDuplicateKeys = false,
            bool useTableLock = true
        ) where T : BaseEntity
        {
            var options = new BulkOperation<T>
            {
                AllowDuplicateKeys = allowDuplicateKeys,
                UseTableLock = useTableLock,
                OnMergeUpdateInputExpression = onMergeUpdateInputExpression,
            };
            return options;
        }
    }
}