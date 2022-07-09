﻿namespace ChuckDeviceConfigurator.Services
{
    using ChuckDeviceController.Data.Entities;

    public interface IAssignmentControllerService
    {
        void Start();

        void Stop();

        void AddAssignment(Assignment assignment);

        void EditAssignment(uint oldAssignmentId, Assignment newAssignment);

        void DeleteAssignment(uint oldAssignmentId);

        void DeleteAssignment(Assignment assignment);
    }
}