namespace ChuckDeviceController.Data;

public class SqlQueryBuilder
{
    public SqlQueryBuilder()
    {
    }

    public string BuildQuery(string sqlQuery, params object[] args)
    {
        var query = string.Format(sqlQuery, args);
        return query;
    }

    /// <summary>
    /// Returns SQL query related to provided query type.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public static (string, string) GetQuery(SqlQueryType type)
    {
        return type switch
        {
            SqlQueryType.AccountOnMergeUpdate => (SqlQueries.AccountOnMergeUpdate, SqlQueries.AccountValuesRaw),
            // Update IV
            //SqlQueryType.PokemonOnMergeUpdate => SqlQueries.PokemonOnMergeUpdate,
            // Do not update IV
            SqlQueryType.PokemonIgnoreOnMerge => (SqlQueries.PokemonIgnoreOnMerge, SqlQueries.PokemonValuesRaw),
            // Insert everything
            SqlQueryType.PokemonOnMergeUpdate => (SqlQueries.PokemonOnMergeUpdate, SqlQueries.PokemonValuesRaw),
            // Do not update quest properties
            SqlQueryType.PokestopIgnoreOnMerge => (SqlQueries.PokestopIgnoreOnMerge, SqlQueries.PokestopValuesRaw),
            SqlQueryType.PokestopOnMergeUpdate => (SqlQueries.PokestopOnMergeUpdate, SqlQueries.PokestopValuesRaw),
            // Only update name/url/updated
            SqlQueryType.PokestopDetailsOnMergeUpdate => (SqlQueries.PokestopDetailsOnMergeUpdate, string.Empty),
            SqlQueryType.IncidentOnMergeUpdate => (SqlQueries.IncidentOnMergeUpdate, SqlQueries.IncidentValuesRaw),
            SqlQueryType.GymOnMergeUpdate => (SqlQueries.GymOnMergeUpdate, SqlQueries.GymValuesRaw),
            // Only update name/url/updated
            SqlQueryType.GymDetailsOnMergeUpdate => (SqlQueries.GymDetailsOnMergeUpdate, string.Empty),
            SqlQueryType.GymDefenderOnMergeUpdate => (SqlQueries.GymDefenderOnMergeUpdate, SqlQueries.GymDefenderValuesRaw),
            SqlQueryType.GymTrainerOnMergeUpdate => (SqlQueries.GymTrainerOnMergeUpdate, SqlQueries.GymTrainerValuesRaw),
            SqlQueryType.SpawnpointOnMergeUpdate => (SqlQueries.SpawnpointOnMergeUpdate, SqlQueries.SpawnpointValuesRaw),
            SqlQueryType.CellOnMergeUpdate => (SqlQueries.CellOnMergeUpdate, SqlQueries.CellValuesRaw),
            SqlQueryType.WeatherOnMergeUpdate => (SqlQueries.WeatherOnMergeUpdate, SqlQueries.WeatherValuesRaw),
            _ => throw new NotImplementedException(),
        };
    }

    //private static DataTable ConvertToDataTable<T>(IEnumerable<T> data)
    //{
    //    var properties = TypeDescriptor.GetProperties(typeof(T));
    //    var dataTable = new DataTable();

    //    foreach (PropertyDescriptor prop in properties)
    //    {
    //        dataTable.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
    //    }

    //    foreach (var item in data)
    //    {
    //        var row = dataTable.NewRow();
    //        foreach (PropertyDescriptor prop in properties)
    //        {
    //            row[prop.Name] = prop.GetValue(item) ?? DBNull.Value;
    //        }
    //        dataTable.Rows.Add(row);
    //    }

    //    return dataTable;
    //}
}