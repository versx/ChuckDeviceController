namespace ChuckDeviceController.Data
{
    public sealed class SqlBulkResult
    {
        public bool Success { get; }

        public int BatchCount { get; }

        public int RowsAffected { get; }

        public int ExpectedCount { get; } // TODO: Remove?

        public SqlBulkResult(bool success, int batchCount, int rowsAffected, int expectedCount)
        {
            Success = success;
            BatchCount = batchCount;
            RowsAffected = rowsAffected;
            ExpectedCount = expectedCount;
        }
    }
}
