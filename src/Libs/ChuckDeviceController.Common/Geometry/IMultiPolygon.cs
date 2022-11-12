namespace ChuckDeviceController.Common.Geometry
{
    public interface IMultiPolygon : IList<IPolygon>
    {
        List<ICoordinate> ConvertToCoordinates();

        List<ulong> GetS2CellIds(ushort minLevel = 15, ushort maxLevel = 15, int maxCells = ushort.MaxValue);

        IBoundingBox GetBoundingBox();
    }
}