using Microsoft.EntityFrameworkCore;

using ChuckDeviceController;
using ChuckDeviceController.Configuration;
using ChuckDeviceController.Data.Contexts;
using ChuckDeviceController.HostedServices;
using ChuckDeviceController.Services;
using ChuckDeviceController.Services.Rpc;


// TODO: Make 'AutomaticMigrations' configurable
const bool AutomaticMigrations = true;

var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
var config = Config.LoadConfig(args, env);
if (config.Providers.Count() == 2)
{
    // Only environment variables and command line providers added,
    // failed to load config provider.
    Environment.FailFast($"Failed to find or load configuration file, exiting...");
}

var connectionString = config.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
var serverVersion = ServerVersion.AutoDetect(connectionString);

// Add services to the container.
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseConfiguration(config);
builder.WebHost.UseUrls(config["Urls"]);

#region Logging Filtering

builder.WebHost.ConfigureLogging(configure =>
{
    var logLevel = config.GetSection("Logging:LogLevel:Default").Get<LogLevel>();
    configure.SetMinimumLevel(logLevel);
    configure.AddSimpleConsole(options =>
    {
        options.IncludeScopes = false;
        options.ColorBehavior = Microsoft.Extensions.Logging.Console.LoggerColorBehavior.Enabled;
    });
    configure.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
    configure.AddFilter("Microsoft.EntityFrameworkCore.Update", LogLevel.None);
    configure.AddFilter("Microsoft.AspNetCore.Diagnostics.DeveloperExceptionPageMiddleware", LogLevel.None);
});

#endregion

#region Services

// Register available DI services
builder.Services.AddSingleton<IProtoProcessorService, ProtoProcessorService>();
builder.Services.AddSingleton<IDataProcessorService, DataProcessorService>();
builder.Services.AddSingleton<IGrpcClientService, GrpcClientService>();
builder.Services.AddSingleton<IBackgroundTaskQueue>(_ =>
{
    // Get max subscription queue capacity config value
    return new DefaultBackgroundTaskQueue(Strings.MaximumQueueCapacity);
});

#endregion

#region Database Contexts

// Register data contexts and factories
builder.Services.AddDbContextFactory<MapDataContext>(options =>
    options.EnableSensitiveDataLogging()
           .UseMySql(connectionString, serverVersion, opt => opt.MigrationsAssembly(Strings.AssemblyName)), ServiceLifetime.Singleton);
builder.Services.AddDbContext<MapDataContext>(options =>
    options.EnableSensitiveDataLogging()
           .UseMySql(connectionString, serverVersion, opt => opt.MigrationsAssembly(Strings.AssemblyName)), ServiceLifetime.Scoped);
builder.Services.AddDbContext<DeviceControllerContext>(options =>
    options.EnableSensitiveDataLogging()
           .UseMySql(connectionString, serverVersion, opt => opt.MigrationsAssembly(Strings.AssemblyName)), ServiceLifetime.Scoped);

#endregion

builder.Services.AddMemoryCache(options =>
{
    // TODO: Make 'CacheSizeLimit' configurable
    options.SizeLimit = 100;
});

#region Hosted Services

// Register available hosted services
builder.Services.AddHostedService<ProtoProcessorService>();
builder.Services.AddHostedService<DataProcessorService>();

#endregion

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var app = builder.Build();

if (AutomaticMigrations)
{
    // Migrate database if needed
    await MigrateDatabase(app.Services);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

app.Run();


static async Task MigrateDatabase(IServiceProvider serviceProvider)
{
    using (var scope = serviceProvider.CreateScope())
    {
        var services = scope.ServiceProvider;
        var loggerFactory = services.GetRequiredService<ILoggerFactory>();
        try
        {
            var mapContext = services.GetRequiredService<MapDataContext>();

            // Migrate the map data tables
            await mapContext.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            var logger = loggerFactory.CreateLogger<Program>();
            logger.LogError(ex, "An error occurred while migrating the database.");
        }
    }
}