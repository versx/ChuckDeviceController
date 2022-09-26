using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

using ChuckDeviceController;
using ChuckDeviceController.Authorization.Jwt.Rpc.Interceptors;
using ChuckDeviceController.Collections.Queues;
using ChuckDeviceController.Configuration;
using ChuckDeviceController.Data.Contexts;
using ChuckDeviceController.Data.Entities;
using ChuckDeviceController.Extensions;
using ChuckDeviceController.Extensions.Data;
using ChuckDeviceController.Extensions.Http.Caching;
using ChuckDeviceController.HostedServices;
using ChuckDeviceController.Services;
using ChuckDeviceController.Services.Rpc;
using ChuckDeviceController.Pvp;

#region Config

var config = Config.LoadConfig(args, Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));
if (config.Providers.Count() == 2)
{
    // Only environment variables and command line providers added,
    // failed to load config provider.
    Environment.FailFast($"Failed to find or load configuration file, exiting...");
}

#endregion

var connectionString = config.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
var serverVersion = ServerVersion.AutoDetect(connectionString);
var logger = new Logger<Program>(LoggerFactory.Create(x => x.AddConsole()));

// Add services to the container.
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseConfiguration(config);
builder.WebHost.UseUrls(config["Urls"]);

#region Logging Filtering

var logLevel = config.GetSection("Logging:LogLevel:Default").Get<LogLevel>();
builder.WebHost.ConfigureLogging(configure =>
    configure.AddSimpleConsole(options =>
        GetLoggingConfig(logLevel, configure)
    )
);

#endregion

#region Services

builder.Services.AddHttpContextAccessor();
builder.Services.Configure<EntityMemoryCacheConfig>(builder.Configuration.GetSection("Cache"));
builder.Services.Configure<GrpcEndpointsConfig>(builder.Configuration.GetSection("Grpc"));
builder.Services.Configure<ProcessorOptionsConfig>(builder.Configuration.GetSection("Options"));

builder.Services.AddSingleton<IAsyncQueue<ProtoPayloadQueueItem>, AsyncQueue<ProtoPayloadQueueItem>>();
builder.Services.AddSingleton<IAsyncQueue<DataQueueItem>, AsyncQueue<DataQueueItem>>();

builder.Services.AddSingleton<IClearFortsHostedService, ClearFortsHostedService>();
builder.Services.AddSingleton<IDataProcessorService, DataProcessorService>();
builder.Services.AddSingleton<IGrpcClientService, GrpcClientService>();
builder.Services.AddSingleton<IMemoryCacheHostedService>(factory =>
{
    using var scope = factory.CreateScope();
    var serviceProvider = scope.ServiceProvider;
    var memCacheOptions = serviceProvider.GetService<IOptions<EntityMemoryCacheConfig>>();
    var memCacheConfig = memCacheOptions?.Value ?? new();
    memCacheConfig.EntityNames = new List<string>
    {
        nameof(Cell),
        nameof(Gym),
        nameof(Incident),
        nameof(Pokemon),
        nameof(Pokestop),
        nameof(Spawnpoint),
        nameof(Weather),
    };
    var memCache = new GenericMemoryCacheHostedService(
        new Logger<IMemoryCacheHostedService>(LoggerFactory.Create(x => x.AddConsole())),
        Options.Create(memCacheConfig)
    );
    return memCache;
});
builder.Services.AddSingleton<IProtoProcessorService, ProtoProcessorService>();

builder.Services.AddSingleton<AuthHeadersInterceptor>();

builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache();

#endregion

#region Database Contexts

// Register data contexts and factories
builder.Services.AddDbContextFactory<MapDbContext>(options =>
    options.GetDbContextOptions(connectionString, serverVersion, Strings.AssemblyName), ServiceLifetime.Singleton);
builder.Services.AddDbContextPool<MapDbContext>(options =>
    options.GetDbContextOptions(connectionString, serverVersion, Strings.AssemblyName));
builder.Services.AddDbContextPool<ControllerDbContext>(options =>
    options.GetDbContextOptions(connectionString, serverVersion, Strings.AssemblyName));

#endregion

#region Hosted Services

// Register available hosted services
builder.Services.AddHostedService<ClearFortsHostedService>();
builder.Services.AddHostedService<DataProcessorService>();
builder.Services.AddHostedService<GenericMemoryCacheHostedService>();
builder.Services.AddHostedService<ProtoProcessorService>();

#endregion

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Instantiate PvpRankGenerator singleton immediately before protos are received
await PvpRankGenerator.Instance.InitializeAsync();

#region App Builder

var app = builder.Build();

// Migrate database to latest migration automatically if enabled
if (config.GetValue<bool>("AutomaticMigrations"))
{
    // Migrate database if needed
    await app.Services.MigrateDatabaseAsync<MapDbContext>();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Convert Map-A-Droid payload data if enabled
if (config.GetValue<bool>("ConvertMadData"))
{
    app.UseMadData();
}

app.UseAuthorization();
app.MapControllers();

app.Run();

#endregion

// TODO: Create extension and move to separate library
static ILoggingBuilder GetLoggingConfig(LogLevel defaultLogLevel, ILoggingBuilder configure)
{
    configure.SetMinimumLevel(defaultLogLevel);
    configure.AddSimpleConsole(options =>
    {
        options.IncludeScopes = false;
        options.ColorBehavior = Microsoft.Extensions.Logging.Console.LoggerColorBehavior.Enabled;
    });
    configure.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
    //configure.AddFilter("Microsoft.EntityFrameworkCore.Model.Validation", LogLevel.Error);
    configure.AddFilter("Microsoft.EntityFrameworkCore.Update", LogLevel.None);
    configure.AddFilter("Microsoft.AspNetCore.Diagnostics.DeveloperExceptionPageMiddleware", LogLevel.None);

    return configure;
}