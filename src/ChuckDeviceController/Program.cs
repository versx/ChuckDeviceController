using System.Diagnostics;

using MicroOrm.Dapper.Repositories;
using MicroOrm.Dapper.Repositories.Config;
using MicroOrm.Dapper.Repositories.SqlGenerator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using MySqlConnector;

using ChuckDeviceController;
using ChuckDeviceController.Authorization.Jwt.Extensions;
using ChuckDeviceController.Collections;
using ChuckDeviceController.Configuration;
using ChuckDeviceController.Data;
using ChuckDeviceController.Data.Contexts;
using ChuckDeviceController.Data.Repositories;
using ChuckDeviceController.Extensions;
using ChuckDeviceController.Extensions.Data;
using ChuckDeviceController.Extensions.Http.Caching;
using ChuckDeviceController.HostedServices;
using ChuckDeviceController.Logging;
using ChuckDeviceController.Protos;
using ChuckDeviceController.Pvp;
using ChuckDeviceController.Services.DataConsumer;
using ChuckDeviceController.Services.DataProcessor;
using ChuckDeviceController.Services.ProtoProcessor;
using ChuckDeviceController.Services.Rpc;


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
var logger = GenericLoggerFactory.CreateLogger<Program>();

#endregion

// Add services to the container.
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseConfiguration(config);
builder.WebHost.UseUrls(config["Urls"]);

#region Logging Filtering

var logLevel = config.GetSection("Logging:LogLevel:Default").Get<LogLevel>();
builder.Logging.ClearProviders();
var loggingConfig = new ColorConsoleLoggerConfiguration();
var loggingSection = config.GetSection("Logging");
var colorLoggingSection = loggingSection.GetSection("ColorConsole");
colorLoggingSection.Bind(loggingConfig);
builder.Logging.AddColorConsoleLogger(options =>
{
    options.LogLevelColorMap = loggingConfig.LogLevelColorMap;
});
builder.Logging.AddFile(loggingSection, options =>
{
    var time = loggingConfig.UseUnix ? DateTime.UtcNow : DateTime.Now;
    options.FormatLogFileName = fileName => string.Format(fileName, time);
    options.UseUtcTimestamp = true;
});
builder.Logging.GetLoggingConfig(logLevel);

#endregion

#region Configuration

builder.Services.Configure<EntityMemoryCacheConfig>(config.GetSection("Cache"));
builder.Services.Configure<GrpcEndpointsConfig>(config.GetSection("Grpc"));
builder.Services.Configure<ProtoProcessorOptionsConfig>(config.GetSection("ProcessingOptions:Protos"));
builder.Services.Configure<DataProcessorOptionsConfig>(config.GetSection("ProcessingOptions:Data"));
builder.Services.Configure<DataConsumerOptionsConfig>(config.GetSection("ProcessingOptions:Consumer"));

var dataOptions = new DataProcessorOptionsConfig();
config.Bind("ProcessingOptions:Data", dataOptions);

var gymOptions = new GymOptions();
var pokestopOptions = new PokestopOptions();
var pokemonOptions = new PokemonOptions();
config.Bind("GymOptions", gymOptions);
config.Bind("PokestopOptions", pokestopOptions);
config.Bind("PokemonOptions", pokemonOptions);
EntityConfiguration.Instance.LoadGymOptions(gymOptions);
EntityConfiguration.Instance.LoadPokestopOptions(pokestopOptions);
EntityConfiguration.Instance.LoadPokemonOptions(pokemonOptions);

var grpcConfig = new GrpcEndpointsConfig();
config.Bind("Grpc", grpcConfig);

#endregion

#region Services

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<SafeCollection<ProtoPayloadQueueItem>>();
builder.Services.AddSingleton<SafeCollection<DataQueueItem>>();

builder.Services.AddSingleton<ClearGymsCache>();
builder.Services.AddSingleton<ClearPokestopsCache>();

builder.Services.AddSingleton<IMemoryCacheHostedService>(factory =>
{
    using var scope = factory.CreateScope();
    var serviceProvider = scope.ServiceProvider;
    var memCacheOptions = serviceProvider.GetService<IOptions<EntityMemoryCacheConfig>>();
    var memCacheConfig = memCacheOptions?.Value ?? new();
    var memCache = new GenericMemoryCacheHostedService(
        GenericLoggerFactory.CreateLogger<IMemoryCacheHostedService>(),
        Options.Create(memCacheConfig)
    );
    return memCache;
});
builder.Services.AddSingleton<IProtoProcessorService, ProtoProcessorService>();
builder.Services.AddSingleton<IDataProcessorService, DataProcessorService>();
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

// Dapper-Repositories registration
// Reference: https://github.com/phnx47/dapper-repositories
MicroOrmConfig.SqlProvider = SqlProvider.MySQL;
MicroOrmConfig.AllowKeyAsIdentity = true;
builder.Services.AddSingleton(typeof(ISqlGenerator<>), typeof(SqlGenerator<>));
builder.Services.AddScoped(typeof(DapperRepository<>), typeof(BaseEntityRepository<>));

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

if (!string.IsNullOrEmpty(grpcConfig.Configurator))
{
    var uri = new Uri(grpcConfig.Configurator);
    // Reference: https://learn.microsoft.com/en-us/aspnet/core/grpc/authn-and-authz?view=aspnetcore-7.0#bearer-token-with-grpc-client-factory
    // Reference[Impl]: https://github.com/dotnet/AspNetCore.Docs/blob/main/aspnetcore/grpc/authn-and-authz/sample/6.x/TicketerClient/Program.cs
    builder.Services
        .AddGrpcClient<Payload.PayloadClient>(options => options.Address = uri)
        .AddCallCredentials(CallCredentialsExtensions.GetAuthorizationToken)
        .ConfigureChannel(options => options.UnsafeUseInsecureChannelCallCredentials = true);
    builder.Services
        .AddGrpcClient<Leveling.LevelingClient>(options => options.Address = uri)
        .AddCallCredentials(CallCredentialsExtensions.GetAuthorizationToken)
        .ConfigureChannel(options => options.UnsafeUseInsecureChannelCallCredentials = true);
    builder.Services
        .AddGrpcClient<WebhookPayload.WebhookPayloadClient>(options => options.Address = uri)
        .AddCallCredentials(CallCredentialsExtensions.GetAuthorizationToken)
        .ConfigureChannel(options => options.UnsafeUseInsecureChannelCallCredentials = true);
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

app.Use((context, next) =>
{
    if (context.Request.ContentType != "application/grpc")
    {
        ProtoDataStatistics.Instance.TotalRequestsProcessed++;
    }
    return next();
});

app.UseAuthorization();
app.MapControllers();

new Thread(async () =>
{
    var stopwatch = new Stopwatch();
    await MonitorResults(TimeSpan.FromMinutes(5), stopwatch);
})
{ IsBackground = true }.Start();

// Open DB connection
var sw = new Stopwatch();
sw.Start();
_ = EntityRepository.InstanceWithOptions(
    dataOptions.EntityInsertConcurrencyLevel,
    dataOptions.EntityQueryConcurrencyLevel,
    dataOptions.EntityQueryWaitTimeS,
    connectionString,
    openConnection: true
);
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

        var instanceCount = EntityRepository.InstanceCount +
            MapDbContext.InstanceCount +
            ControllerDbContext.InstanceCount;
        var requestCount = ProtoDataStatistics.Instance.TotalRequestsProcessed;
        var elapsed = stopwatch.Elapsed;
        var currentElapsed = elapsed - lastElapsed;
        var currentRequests = requestCount - lastRequestCount;

        Console.WriteLine(
            $"[{DateTime.Now:HH:mm:ss.fff}] "
            + $"Database connections/s: {instanceCount - lastInstanceCount} | "
            + $"Requests/s: {Math.Round(currentRequests / currentElapsed.TotalSeconds)}");

        lastInstanceCount = instanceCount;
        lastRequestCount = requestCount;
        lastElapsed = elapsed;
    }

    Console.WriteLine();
    Console.WriteLine($"Total database connections created: {EntityRepository.InstanceCount}");
    Console.WriteLine(
        $"Requests per second:     {Math.Round(ProtoDataStatistics.Instance.TotalRequestsProcessed / stopwatch.Elapsed.TotalSeconds)}");

    stopwatch.Stop();
}