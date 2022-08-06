namespace ChuckDeviceController.Plugins
{
    using ChuckDeviceController.Common.Data;
    using ChuckDeviceController.Common.Jobs;

    public interface IJobControllerServiceHost
    {
        // TODO: Possibly include list of registered job controller types
        // TODO: Also include loaded devices and instances dictionaries

        // TODO: Re-think type registration for plugins, unless we want to use codeDOM and dynamically generate a lib for InstanceType and reload everything (nope, nope, nope)
        //Task RegisterAllJobControllerTypesAsync();

        //Task RegisterJobControllerTypeAsync(InstanceType type);

        Task AddJobControllerAsync(string name, IJobController controller);
    }
}