namespace ChuckDeviceController.Geometry.Models.Contracts
{
    public interface IMultiPolygon : IList<IPolygon>
    {
        IReadOnlyList<ICoordinate> ToCoordinates();

        //IReadOnlyList<ulong> GetS2CellIds(ushort minLevel = 15, ushort maxLevel = 15, int maxCells = ushort.MaxValue);

        IBoundingBox GetBoundingBox();
    }
}