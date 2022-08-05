namespace ChuckDeviceController.Common.Data.Contracts
{
    public interface IAssignmentGroup : IBaseEntity
    {
        string Name { get; }

        IList<uint> AssignmentIds { get; }
    }
}