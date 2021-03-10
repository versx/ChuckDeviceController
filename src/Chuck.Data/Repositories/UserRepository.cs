namespace Chuck.Data.Repositories
{
    using System;
    using System.Threading.Tasks;

    using Chuck.Data.Contexts;
    using Chuck.Data.Entities;
    using Chuck.Extensions;

    public class UserRepository : EfCoreRepository<User, DeviceControllerContext>
    {
        public UserRepository(DeviceControllerContext context)
            : base(context)
        {
        }

        public async Task<User> CreateRootAccount(string password)
        {
            var now = DateTime.UtcNow.ToTotalSeconds();
            var user = new User
            {
                Username = "root",
                Password = password,
                Permissions = Permission.Admin | Permission.Accounts |
                              Permission.Assignments | Permission.DeviceGroups |
                              Permission.Devices | Permission.Geofences |
                              Permission.Instances | Permission.IVLists |
                              Permission.Reserved | Permission.Settings |
                              Permission.Utilities | Permission.Webhooks,
                Created = now,
                Updated = now,
                //IsRoot = true,
                Enabled = true,
            };
            return await AddAsync(user).ConfigureAwait(false);
        }
    }
}