namespace ChuckDeviceController.Common.Abstractions;

public interface IAssignmentGroup : IBaseEntity
{
    string Name { get; }

    List<uint> AssignmentIds { get; }
}