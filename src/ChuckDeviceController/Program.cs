using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

using ChuckDeviceController;
using ChuckDeviceController.Authorization.Jwt.Rpc.Interceptors;
using ChuckDeviceController.Collections.Queues;
using ChuckDeviceController.Configuration;
using ChuckDeviceController.Data.Contexts;
using ChuckDeviceController.Extensions;
using ChuckDeviceController.Extensions.Data;
using ChuckDeviceController.HostedServices;
using ChuckDeviceController.Services;
using ChuckDeviceController.Services.Rpc;


#region Config

var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
var config = Config.LoadConfig(args, env);
if (config.Providers.Count() == 2)
{
    // Only environment variables and command line providers added,
    // failed to load config provider.
    Environment.FailFast($"Failed to find or load configuration file, exiting...");
}

#endregion

//var logger = new Logger<Program>(LoggerFactory.Create(x => x.AddConsole()));
var connectionString = config.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
var serverVersion = ServerVersion.AutoDetect(connectionString);

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

builder.Services.AddSingleton<IAsyncQueue<ProtoPayloadQueueItem>>(_ => new AsyncQueue<ProtoPayloadQueueItem>());
builder.Services.AddSingleton<IAsyncQueue<List<dynamic>>>(_ => new AsyncQueue<List<dynamic>>());

//builder.Services.AddScoped<IMemoryCacheHostedService, MemoryCacheHostedService>();
builder.Services.AddSingleton<IMemoryCacheHostedService, MemoryCacheHostedService>();

builder.Services.AddSingleton<IClearFortsHostedService, ClearFortsHostedService>();
builder.Services.AddSingleton<IDataProcessorService, DataProcessorService>();
builder.Services.AddSingleton<IGrpcClientService, GrpcClientService>();
builder.Services.AddSingleton<IProtoProcessorService, ProtoProcessorService>();
builder.Services.Configure<ProcessorOptionsConfig>(builder.Configuration.GetSection("Options"));

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<AuthHeadersInterceptor>();

var memCacheOptions = new MemoryCacheOptions
{
    // TODO: Make 'CacheSizeLimit' configurable
    SizeLimit = 10240,
    ExpirationScanFrequency = TimeSpan.FromMinutes(60),
    CompactionPercentage = 0.50,
};
builder.Services.AddMemoryCache(options => options = memCacheOptions);
builder.Services.AddDistributedMemoryCache(options => options = (MemoryDistributedCacheOptions)memCacheOptions);

#endregion

#region Database Contexts

// Register data contexts and factories
builder.Services.AddDbContextFactory<MapDbContext>(options =>
    options.GetDbContextOptions(connectionString, serverVersion, Strings.AssemblyName), ServiceLifetime.Singleton);
builder.Services.AddDbContext<MapDbContext>(options =>
    options.GetDbContextOptions(connectionString, serverVersion, Strings.AssemblyName), ServiceLifetime.Singleton);
builder.Services.AddDbContext<ControllerDbContext>(options =>
    options.GetDbContextOptions(connectionString, serverVersion, Strings.AssemblyName), ServiceLifetime.Scoped);

#endregion

#region Hosted Services

// Register available hosted services
builder.Services.AddHostedService<ClearFortsHostedService>();
builder.Services.AddHostedService<DataProcessorService>();
builder.Services.AddHostedService<MemoryCacheHostedService>();
builder.Services.AddHostedService<ProtoProcessorService>();

#endregion

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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