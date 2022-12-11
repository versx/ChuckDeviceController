using System.Diagnostics;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using MySqlConnector;

using ChuckDeviceController;
using ChuckDeviceController.Authorization.Jwt.Rpc.Interceptors;
using ChuckDeviceController.Collections;
using ChuckDeviceController.Configuration;
using ChuckDeviceController.Data.Contexts;
using ChuckDeviceController.Data.Repositories;
using ChuckDeviceController.Extensions;
using ChuckDeviceController.Extensions.Data;
using ChuckDeviceController.Extensions.Http.Caching;
using ChuckDeviceController.HostedServices;
using ChuckDeviceController.Logging;
using ChuckDeviceController.Protos;
using ChuckDeviceController.Pvp;
using ChuckDeviceController.Services;
using ChuckDeviceController.Services.Rpc;

// TODO: IAccountStatusHostedService - Loop all accounts with `first_warning_timestamp`, `failed` or `failed_timestamp` set and check if warning/suspension is lifted. If so, clear it.


#region Config

var config = Config.LoadConfig(args, Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));
if (config.Providers.Count() == 2)
{
    // Only environment variables and command line providers added,
    // failed to load config provider.
    Environment.FailFast($"Failed to find or load configuration file, exiting...");
}

// MySQL resiliency options
var resiliencyConfig = new MySqlResiliencyOptions();
config.Bind("Database", resiliencyConfig);

var connectionString = config.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
var serverVersion = ServerVersion.AutoDetect(connectionString);
var logger = new Logger<Program>(LoggerFactory.Create(x => x.AddConsole()));

#endregion

// Add services to the container.
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseConfiguration(config);
builder.WebHost.UseUrls(config["Urls"]);

#region Logging Filtering

var logLevel = config.GetSection("Logging:LogLevel:Default").Get<LogLevel>();
builder.WebHost.ConfigureLogging(configure =>
{
    configure.ClearProviders();
    var loggingSection = config.GetSection("Logging");
    var colorMap = new Dictionary<LogLevel, ConsoleColor>();
    var colorLoggingSection = loggingSection.GetSection("ColorConsole:LogLevelColorMap");
    colorLoggingSection.Bind(colorMap);
    configure.AddColorConsoleLogger(options =>
    {
        options.LogLevelColorMap = colorMap;
    });
    configure.AddFile(loggingSection, options =>
    {
        options.FormatLogFileName = fileName => string.Format(fileName, DateTime.UtcNow);
    });
    configure.GetLoggingConfig(logLevel);
});

#endregion

#region Configuration

builder.Services.Configure<EntityMemoryCacheConfig>(config.GetSection("Cache"));
builder.Services.Configure<GrpcEndpointsConfig>(config.GetSection("Grpc"));
builder.Services.Configure<ProtoProcessorOptionsConfig>(config.GetSection("ProcessingOptions:Protos"));
builder.Services.Configure<DataProcessorOptionsConfig>(config.GetSection("ProcessingOptions:Data"));
builder.Services.Configure<DataConsumerOptionsConfig>(config.GetSection("ProcessingOptions:Consumer"));

#endregion

#region Services

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<SafeCollection<ProtoPayloadQueueItem>>();
builder.Services.AddSingleton<SafeCollection<DataQueueItem>>();

builder.Services.AddSingleton<IClearFortsHostedService, ClearFortsHostedService>();
builder.Services.AddSingleton<IDataProcessorService, DataProcessorService>();

builder.Services.AddSingleton<IMemoryCacheHostedService>(factory =>
{
    using var scope = factory.CreateScope();
    var serviceProvider = scope.ServiceProvider;
    var memCacheOptions = serviceProvider.GetService<IOptions<EntityMemoryCacheConfig>>();
    var memCacheConfig = memCacheOptions?.Value ?? new();
    var memCache = new GenericMemoryCacheHostedService(
        new Logger<IMemoryCacheHostedService>(LoggerFactory.Create(x => x.AddConsole())),
        Options.Create(memCacheConfig)
    );
    return memCache;
});
builder.Services.AddSingleton<IProtoProcessorService, ProtoProcessorService>();
builder.Services.AddSingleton<IDataConsumerService, DataConsumerService>();

builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache();

#endregion

#region Database Contexts

// Register data contexts, factories, and pools
builder.Services.AddDbContext<MapDbContext>(options =>
    options.GetDbContextOptions(connectionString, serverVersion, Strings.AssemblyName, resiliencyConfig), ServiceLifetime.Scoped);
builder.Services.AddScoped<MySqlConnection>(options =>
{
    var connection = new MySqlConnection(connectionString);
    //Task.Run(async () => await connection.OpenAsync()).Wait();
    connection.Open();
    return connection;
});

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

#region gRPC Clients

var grpcConfig = new GrpcEndpointsConfig();
config.Bind("Grpc", grpcConfig);

builder.Services.AddSingleton<IGrpcClient<Payload.PayloadClient, PayloadRequest, PayloadResponse>, GrpcProtoClient>();
builder.Services.AddSingleton<IGrpcClient<Leveling.LevelingClient, TrainerInfoRequest, TrainerInfoResponse>, GrpcLevelingClient>();
builder.Services.AddSingleton<IGrpcClient<WebhookPayload.WebhookPayloadClient, WebhookPayloadRequest, WebhookPayloadResponse>, GrpcWebhookClient>();
builder.Services.AddSingleton<AuthHeadersInterceptor>();

if (!string.IsNullOrEmpty(grpcConfig.Configurator))
{
    builder.Services
        .AddGrpcClient<Payload.PayloadClient>(options => options.Address = new Uri(grpcConfig.Configurator))
        .AddInterceptor<AuthHeadersInterceptor>();
    builder.Services
        .AddGrpcClient<Leveling.LevelingClient>(options => options.Address = new Uri(grpcConfig.Configurator))
        .AddInterceptor<AuthHeadersInterceptor>();
}
if (!string.IsNullOrEmpty(grpcConfig.Communicator))
{
    builder.Services
        .AddGrpcClient<WebhookPayload.WebhookPayloadClient>(options => options.Address = new Uri(grpcConfig.Communicator))
        // TODO: Add gRPC JWT auth token retrieval to client registration
        //.AddCallCredentials(async (context, metadata, serviceProvider) =>
        //{
        //    var provider = serviceProvider.GetRequiredService<ITokenProvider>();
        //    var token = await provider.GetJwtTokenAsync();
        //    metadata.Add("Authorization", $"Bearer {token}");
        //    //return await Task.CompletedTask;
        //})
        .AddInterceptor<AuthHeadersInterceptor>();
}

#endregion

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
    app.UseMadDataConverter();
}

app.UseAuthorization();
app.MapControllers();

//new Thread(async () =>
//{
//    var stopwatch = new Stopwatch();
//    await MonitorResults(TimeSpan.FromMinutes(5), stopwatch);
//})
//{ IsBackground = true }.Start();

// Open DB connection
var sw = new Stopwatch();
sw.Start();
_ = EntityRepository.InstanceWithOptions(connectionString, openConnection: true);
sw.Stop();
var totalSeconds = Math.Round(sw.Elapsed.TotalSeconds, 4);
logger.LogDebug($"Opening database connection took {totalSeconds}s");

app.Run();

#endregion

static async Task MonitorResults(TimeSpan duration, Stopwatch stopwatch)
{
    var lastInstanceCount = 0UL;
    var lastRequestCount = 0UL;
    var lastElapsed = TimeSpan.Zero;

    stopwatch.Start();

    while (stopwatch.Elapsed < duration)
    {
        await Task.Delay(TimeSpan.FromSeconds(1));

        var instanceCount = MapDbContext.InstanceCount;
        var requestCount = ProtoDataStatistics.Instance.TotalRequestsProcessed;
        var elapsed = stopwatch.Elapsed;
        var currentElapsed = elapsed - lastElapsed;
        var currentRequests = requestCount - lastRequestCount;

        Console.WriteLine(
            $"[{DateTime.Now:HH:mm:ss.fff}] "
            + $"Context creations/second: {instanceCount - lastInstanceCount} | "
            + $"Requests/second: {Math.Round(currentRequests / currentElapsed.TotalSeconds)}");

        lastInstanceCount = instanceCount;
        lastRequestCount = requestCount;
        lastElapsed = elapsed;
    }

    Console.WriteLine();
    Console.WriteLine($"Total context creations: {MapDbContext.InstanceCount}");
    Console.WriteLine(
        $"Requests per second:     {Math.Round(ProtoDataStatistics.Instance.TotalRequestsProcessed / stopwatch.Elapsed.TotalSeconds)}");

    stopwatch.Stop();
}