namespace ChuckDeviceController.Common.Data.Contracts
{
    public interface IAssignmentGroup : IBaseEntity
    {
        string Name { get; }

        List<uint> AssignmentIds { get; }
    }
}