﻿namespace ChuckDeviceController.Plugin;

/// <summary>
/// Interface contract for grouping settings properties.
/// </summary>
public interface ISettingsPropertyGroup
{
    /// <summary>
    /// Gets or sets the unique ID for the settings property group.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets or sets the display text for the settings property group.
    /// </summary>
    string Text { get; }

    /// <summary>
    /// Gets or sets a value used for sorting each HTML element
    /// created for the properties.
    /// </summary>
    uint DisplayIndex { get; }
}