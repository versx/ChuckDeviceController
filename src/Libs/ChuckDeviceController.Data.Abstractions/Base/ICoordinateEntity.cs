namespace ChuckDeviceController.Data.Abstractions;

//using Microsoft.EntityFrameworkCore;

public interface ICoordinateEntity
{
    // TODO: [Precision(18, 6)]
    double Latitude { get; }

    //[Precision(18, 6)]
    double Longitude { get; }
}