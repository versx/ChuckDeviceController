using ChuckDeviceController;
using ChuckDeviceController.Data.Contexts;
using ChuckDeviceController.HostedServices;
using ChuckDeviceController.Services;

using Microsoft.EntityFrameworkCore;


// TODO: Make configurable
const bool AutomaticMigrations = true;

var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
var config = LoadConfig(env);
if (config.Providers.Count() == 2)
{
    // Only environment variables and command line providers added,
    // failed to load config provider.
    Environment.FailFast($"Failed to find or load {Strings.AppSettings} configuration file, exiting...");
}

var connectionString = config.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
var serverVersion = ServerVersion.AutoDetect(connectionString);

// Add services to the container.
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseConfiguration(config);
builder.WebHost.UseUrls(config["Urls"]);

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

// Register available DI services
builder.Services.AddSingleton<IProtoProcessorService, ProtoProcessorService>();
builder.Services.AddSingleton<IDataProcessorService, DataProcessorService>();
builder.Services.AddSingleton<IDataConsumer, DataConsumer>();
builder.Services.AddSingleton<IBackgroundTaskQueue>(_ =>
{
    // Get max subscription queue capacity config value
    return new DefaultBackgroundTaskQueue(Strings.MaximumQueueCapacity);
});

// Register data contexts and factories
builder.Services.AddDbContextFactory<MapDataContext>(options =>
{
    options.EnableSensitiveDataLogging()
           .UseMySql(connectionString, serverVersion, opt =>
           {
               opt.MigrationsAssembly(Strings.AssemblyName);
           });
}, ServiceLifetime.Singleton);
builder.Services.AddDbContext<MapDataContext>(options =>
{
    options.EnableSensitiveDataLogging()
           .UseMySql(connectionString, serverVersion, opt =>
           {
               opt.MigrationsAssembly(Strings.AssemblyName);
           });
}, ServiceLifetime.Scoped);
builder.Services.AddDbContext<DeviceControllerContext>(options =>
{
    options.EnableSensitiveDataLogging()
           .UseMySql(connectionString, serverVersion, opt =>
           {
               opt.MigrationsAssembly(Strings.AssemblyName);
           });
}, ServiceLifetime.Scoped);

builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 100;
});

// Register available hosted services
builder.Services.AddHostedService<ProtoProcessorService>();
builder.Services.AddHostedService<DataProcessorService>();

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



// TODO: Move to shared class
IConfigurationRoot LoadConfig(string env = "")
{
    var baseFilePath = Path.Combine(Strings.BasePath, Strings.AppSettings);
    var envFilePath = Path.Combine(Strings.BasePath, string.Format(Strings.AppSettingsFormat, env));

    var configBuilder = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory());
    if (File.Exists(baseFilePath))
    {
        configBuilder = configBuilder.AddJsonFile(baseFilePath, optional: false, reloadOnChange: true);
    }
    if (File.Exists(envFilePath))
    {
        configBuilder = configBuilder.AddJsonFile(envFilePath, optional: true, reloadOnChange: true);
    }
    var config = configBuilder.AddEnvironmentVariables()
        .AddCommandLine(args)
        .Build();
    return config;
}

async Task MigrateDatabase(IServiceProvider serviceProvider)
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