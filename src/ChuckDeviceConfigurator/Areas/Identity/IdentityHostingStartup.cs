[assembly: HostingStartup(typeof(ChuckDeviceConfigurator.Areas.Identity.IdentityHostingStartup))]
namespace ChuckDeviceConfigurator.Areas.Identity
{
    using Microsoft.EntityFrameworkCore;

    using ChuckDeviceConfigurator.Data;

    public class IdentityHostingStartup : IHostingStartup
    {
        private readonly ILogger<IdentityHostingStartup> _logger;

        public IdentityHostingStartup(ILogger<IdentityHostingStartup> logger)
        {
            _logger = logger;
        }

        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices((context, services) =>
            {
                services.AddDbContext<UserIdentityContext>(options =>
                {
                    try
                    {
                        var connectionString = context.Configuration.GetSection("ConnectionStrings:DefaultConnection").Get<string>();
                        var serverVersion = ServerVersion.AutoDetect(connectionString);
                        options.UseMySql(connectionString, serverVersion);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Failed to make connection with database: {ex}");
                    }
                });
            });
        }
    }
}