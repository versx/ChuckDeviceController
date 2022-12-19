namespace ChuckDeviceController.Data.Entities;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using ChuckDeviceController.Data.Abstractions;
using ChuckDeviceController.Data.Common;

[Table("plugin")]
public class Plugin : BaseEntity, IPluginState
{
    [
        Column("name"),
        Key,
        DatabaseGenerated(DatabaseGeneratedOption.None),
    ]
    public string Name { get; set; } = null!;

    [Column("full_path")]
    public string FullPath { get; set; } = null!;

    [Column("state")]
    public PluginState State { get; set; }

    #region Helper Methods

    public static string PluginStateToString(PluginState state)
    {
        return state switch
        {
            //PluginState.Unset => "unset",
            PluginState.Running => "running",
            PluginState.Stopped => "stopped",
            PluginState.Disabled => "disabled",
            PluginState.Removed => "removed",
            PluginState.Error => "error",
            //_ => state.ToString(),
            _ => "unset",
        };
    }

    public static PluginState StringToPluginState(string pluginState)
    {
        return pluginState.ToLower() switch
        {
            //"unset" => PluginState.Unset,
            "running" => PluginState.Running,
            "stopped" => PluginState.Stopped,
            "disabled" => PluginState.Disabled,
            "removed" => PluginState.Removed,
            "error" => PluginState.Error,
            _ => PluginState.Unset,
        };
    }

    #endregion
}