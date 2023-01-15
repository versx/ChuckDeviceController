namespace ChuckDeviceController.Data.TypeHandlers;

using System.ComponentModel.DataAnnotations.Schema;

using Dapper;

using ChuckDeviceController.Common;
using ChuckDeviceController.Data.Entities;

public static class DapperTypeMappings
{
    public static void AddTypeMappers()
    {
        SetTypeMap<Account>();
        SetTypeMap<ApiKey>();
        SetTypeMap<Assignment>();
        SetTypeMap<AssignmentGroup>();
        SetTypeMap<Device>();
        SetTypeMap<DeviceGroup>();
        SetTypeMap<Geofence>();
        SetTypeMap<Instance>();
        SetTypeMap<IvList>();
        SetTypeMap<Webhook>();

        SetTypeMap<Cell>();
        SetTypeMap<Gym>();
        SetTypeMap<GymDefender>();
        SetTypeMap<GymTrainer>();
        SetTypeMap<Incident>();
        SetTypeMap<Pokestop>();
        SetTypeMap<Pokemon>();
        SetTypeMap<Spawnpoint>();
        SetTypeMap<Weather>();

        SqlMapper.AddTypeHandler(new JsonTypeHandler<List<Dictionary<string, dynamic>>>()); // Quest.Rewards / Quest.Conditions
        SqlMapper.AddTypeHandler(new JsonTypeHandler<Dictionary<string, dynamic>>()); // Pokemon.Pvp

        SqlMapper.AddTypeHandler(typeof(InstanceType), InstanceTypeTypeHandler.Default);
        SqlMapper.AddTypeHandler(typeof(SeenType), SeenTypeTypeHandler.Default);
        //SqlMapper.AddTypeHandler(typeof(WebhookTypeTypeHandler), WebhookTypeTypeHandler.Default);
        SqlMapper.AddTypeHandler(new JsonTypeHandler<List<string>>()); // Instance.Geofences / Webhook.Geofences / IvList.PokemonIds
        SqlMapper.AddTypeHandler(new JsonTypeHandler<List<uint>>()); // AssignmentGroup.AssignmentIds
        SqlMapper.AddTypeHandler(new JsonTypeHandler<List<WebhookType>>()); // Webhook.Types
        SqlMapper.AddTypeHandler(new JsonTypeHandler<GeofenceData>());
        SqlMapper.AddTypeHandler(new JsonTypeHandler<InstanceData>());
        SqlMapper.AddTypeHandler(new JsonTypeHandler<WebhookData>());
    }

    public static void SetTypeMap<TEntity>() => SetTypeMap(typeof(TEntity));

    public static void SetTypeMap(Type type)
    {
        SqlMapper.SetTypeMap(
            type,
            new CustomPropertyTypeMap(
                type,
                (type, columnName) =>
                    type.GetProperties().FirstOrDefault(prop =>
                        prop.GetCustomAttributes(false)
                            .OfType<ColumnAttribute>()
                            .Any(attr => attr.Name == columnName))));
    }
}