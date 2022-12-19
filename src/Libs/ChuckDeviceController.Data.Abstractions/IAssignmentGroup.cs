namespace ChuckDeviceController.Data.Abstractions;

public interface IAssignmentGroup : IBaseEntity
{
    string Name { get; }

    List<uint> AssignmentIds { get; }
}