namespace ChuckDeviceController.Common.Jobs;

using ChuckDeviceController.Geometry.Models.Abstractions;

/// <summary>
/// Allows for 'ScanNext' API scanning.
/// </summary>
public interface IScanNextInstanceController
{
    /// <summary>
    /// Gets a queue of coordinates to pokemon to scan.
    /// </summary>
    Queue<ICoordinate> ScanNextCoordinates { get; }
}