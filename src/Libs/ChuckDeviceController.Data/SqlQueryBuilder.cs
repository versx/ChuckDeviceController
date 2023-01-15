namespace ChuckDeviceController.Data;

public class SqlQueryBuilder
{
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
            SqlQueryType.AccountUpdateOnMerge => (SqlQueries.AccountOnMergeUpdate, SqlQueries.AccountValuesRaw),
            // Update IV
            //SqlQueryType.PokemonOnMergeUpdate => SqlQueries.PokemonOnMergeUpdate,
            // Do not update IV
            SqlQueryType.PokemonIgnoreOnMerge => (SqlQueries.PokemonIgnoreOnMerge, SqlQueries.PokemonValuesRaw),
            // Insert everything
            SqlQueryType.PokemonUpdateOnMerge => (SqlQueries.PokemonOnMergeUpdate, SqlQueries.PokemonValuesRaw),
            // Do not update quest properties
            SqlQueryType.PokestopIgnoreOnMerge => (SqlQueries.PokestopIgnoreOnMerge, SqlQueries.PokestopValuesRaw),
            SqlQueryType.PokestopUpdateOnMerge => (SqlQueries.PokestopOnMergeUpdate, SqlQueries.PokestopValuesRaw),
            // Only update name/url/updated
            SqlQueryType.PokestopDetailsUpdateOnMerge => (SqlQueries.PokestopDetailsOnMergeUpdate, string.Empty),
            SqlQueryType.IncidentUpdateOnMerge => (SqlQueries.IncidentOnMergeUpdate, SqlQueries.IncidentValuesRaw),
            SqlQueryType.GymUpdateOnMerge => (SqlQueries.GymOnMergeUpdate, SqlQueries.GymValuesRaw),
            // Only update name/url/updated
            SqlQueryType.GymDetailsUpdateOnMerge => (SqlQueries.GymDetailsOnMergeUpdate, string.Empty),
            SqlQueryType.GymDefenderUpdateOnMerge => (SqlQueries.GymDefenderOnMergeUpdate, SqlQueries.GymDefenderValuesRaw),
            SqlQueryType.GymTrainerUpdateOnMerge => (SqlQueries.GymTrainerOnMergeUpdate, SqlQueries.GymTrainerValuesRaw),
            SqlQueryType.SpawnpointUpdateOnMerge => (SqlQueries.SpawnpointOnMergeUpdate, SqlQueries.SpawnpointValuesRaw),
            SqlQueryType.CellUpdateOnMerge => (SqlQueries.CellOnMergeUpdate, SqlQueries.CellValuesRaw),
            SqlQueryType.WeatherUpdateOnMerge => (SqlQueries.WeatherOnMergeUpdate, SqlQueries.WeatherValuesRaw),
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