namespace Chuck.Data.Repositories
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Chuck.Data.Contexts;
    using Chuck.Data.Entities;

    public class InstanceRepository : EfCoreRepository<Instance, DeviceControllerContext>
    {
        private readonly GeofenceRepository _geofenceRepository;
        private readonly PokestopRepository _pokestopRepository;

        public InstanceRepository(DeviceControllerContext context)
            : base(context)
        {
            _geofenceRepository = new GeofenceRepository(_dbContext);
            _pokestopRepository = new PokestopRepository(_dbContext);
        }

        public async Task ClearQuests(Instance instance, List<List<double>> coordinates) // TODO: Coordinate
        {
            var geofences = await _geofenceRepository.GetByIdsAsync(instance.Geofences);
            var pokestops = await _pokestopRepository.GetAllAsync();
            var clearablePokestops = new List<Pokestop>();
            foreach (var stop in pokestops)
            {
                // TODO: Check if pokestop is within any geofence
                // TODO: If so, add to clearablePokestops list
            }
            // TODO: Clear pokestops list
        }
    }
}