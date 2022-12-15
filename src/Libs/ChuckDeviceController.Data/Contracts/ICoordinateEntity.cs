namespace ChuckDeviceController.Data.Contracts
{
    using Microsoft.EntityFrameworkCore;

    public interface ICoordinateEntity
    {
        [Precision(18, 6)]
        double Latitude { get; }

        [Precision(18, 6)]
        double Longitude { get; }
    }
}