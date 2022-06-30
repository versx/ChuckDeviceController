[assembly: HostingStartup(typeof(ChuckDeviceConfigurator.Areas.Identity.IdentityHostingStartup))]
namespace ChuckDeviceConfigurator.Areas.Identity
{
    using Microsoft.EntityFrameworkCore;

    using ChuckDeviceConfigurator.Data;

    public class IdentityHostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices((context, services) =>
            {
                services.AddDbContext<UserIdentityContext>(options =>
                {
                    var connectionString = context.Configuration.GetSection("ConnectionStrings:DefaultConnection").Get<string>();
                    var serverVersion = ServerVersion.AutoDetect(connectionString);
                    options.UseMySql(connectionString, serverVersion);
                });
            });
        }
    }
}