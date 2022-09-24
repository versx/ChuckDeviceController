namespace ChuckDeviceController.Common.Data.Contracts
{
    using System.Text.Json.Serialization;

    public interface IApiKey
    {
        uint Id { get; }

        string? Key { get; }

        List<PluginApiKeyScope>? Scope { get; }

        ulong ExpirationTimestamp { get; }
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PluginApiKeyScope
    {
        /// <summary>
        /// No extra permissions
        /// </summary>
        None,

        /// <summary>
        /// Read database entities
        /// </summary>
        ReadDatabase,

        /// <summary>
        /// Write database entities
        /// </summary>
        WriteDatabase,

        /// <summary>
        /// Delete database entities (NOTE: Should probably remove since Delete == Write essentially but would be nice to separate it)
        /// </summary>
        DeleteDatabase,

        /// <summary>
        /// Add new ASP.NET Mvc controller routes
        /// </summary>
        AddControllers,

        /// <summary>
        /// Add new job controller instances for devices
        /// </summary>
        AddJobControllers,

        /// <summary>
        /// Add new instances
        /// </summary>
        AddInstances,
    }
}