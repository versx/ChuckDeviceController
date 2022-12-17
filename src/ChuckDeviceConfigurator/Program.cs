using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

using ChuckDeviceConfigurator;
using ChuckDeviceConfigurator.Data;
using ChuckDeviceConfigurator.Extensions;
using ChuckDeviceConfigurator.Localization;
using ChuckDeviceConfigurator.Services.Assignments;
using ChuckDeviceConfigurator.Services.Geofences;
using ChuckDeviceConfigurator.Services.IvLists;
using ChuckDeviceConfigurator.Services.Jobs;
using ChuckDeviceConfigurator.Services.Net.Mail;
using ChuckDeviceConfigurator.Services.Plugins;
using ChuckDeviceConfigurator.Services.Plugins.Hosts;
using ChuckDeviceConfigurator.Services.Plugins.Hosts.EventBusService;
using ChuckDeviceConfigurator.Services.Plugins.Hosts.EventBusService.Publishers;
using ChuckDeviceConfigurator.Services.Rpc;
using ChuckDeviceConfigurator.Services.TimeZone;
using ChuckDeviceConfigurator.Services.Webhooks;
using ChuckDeviceController.Authorization.Jwt.Middleware;
using ChuckDeviceController.Configuration;
using ChuckDeviceController.Data.Contexts;
using ChuckDeviceController.Data.Entities;
using ChuckDeviceController.Data.Repositories;
using ChuckDeviceController.Extensions.Data;
using ChuckDeviceController.Extensions.Http.Caching;
using ChuckDeviceController.Logging;
using ChuckDeviceController.Plugin;
using ChuckDeviceController.Plugin.EventBus;
using ChuckDeviceController.Plugin.EventBus.Observer;
using ChuckDeviceController.PluginManager;
using ChuckDeviceController.PluginManager.Mvc.Extensions;
using ChuckDeviceController.Routing;


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

// JWT authentication options for gRPC endpoints
var jwtConfig = new JwtAuthConfig();
config.Bind("Jwt", jwtConfig);

// User identity options
var identityConfig = GetDefaultIdentityOptions();
config.Bind("UserAccounts", identityConfig);

#endregion

#region Logger

var logger = GenericLoggerFactory.CreateLogger<Program>();
// Need to call at startup so time gets set now and not when first visit to dashboard
var started = Strings.Uptime.ToLocalTime();
logger.LogInformation($"Started: {started.ToLongDateString()} {started.ToLongTimeString()}");

#endregion

// Create locale translation files
try
{
    await Translator.CreateLocaleFilesAsync();
    var locale = config.GetValue<string>("Locale") ?? "en";
    Translator.Instance.SetLocale(locale);
}
catch (Exception ex)
{
    logger.LogError($"Failed to generate locale files, make sure the base locales exist: {ex}");
}

var connectionString = config.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
var serverVersion = ServerVersion.AutoDetect(connectionString);

// Add services to the container.
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseConfiguration(config);

#region Logger Filtering

var logLevel = config.GetSection("Logging:LogLevel:Default").Get<LogLevel>();
builder.WebHost.ConfigureLogging(configure =>
{
    configure.ClearProviders();
    var loggingSection = config.GetSection("Logging");
    var loggingConfig = new ColorConsoleLoggerConfiguration();
    var colorLoggingSection = loggingSection.GetSection("ColorConsole");
    colorLoggingSection.Bind(loggingConfig);
    configure.AddColorConsoleLogger(options =>
    {
        options.LogLevelColorMap = loggingConfig.LogLevelColorMap;
    });
    configure.AddFile(loggingSection, options =>
    {
        var time = loggingConfig.UseUnix ? DateTime.UtcNow : DateTime.Now;
        options.FormatLogFileName = fileName => string.Format(fileName, time);
        options.UseUtcTimestamp = true;
    });
    configure.GetLoggingConfig(logLevel);
});

#endregion

#region User Identity

// https://codewithmukesh.com/blog/user-management-in-aspnet-core-mvc/
builder.Services.AddDbContext<UserIdentityContext>(options =>
    options.GetDbContextOptions(connectionString, serverVersion, Strings.AssemblyName, resiliencyConfig), ServiceLifetime.Transient);

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options => options = identityConfig)
    .AddDefaultUI()
    .AddEntityFrameworkStores<UserIdentityContext>()
    .AddDefaultTokenProviders();

builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = long.MaxValue;
});

// Register external 3rd party authentication providers if configured
builder.Services
    .AddAuthorization(options =>
    {
        //options.AddPolicy("GrpcAuthenticationServices", policy =>
        //{
        //    policy.RequireRole("Grpc");
        //    policy.RequireAssertion(context =>
        //    {
        //        var result = context.User.HasClaim(claim => claim.Type == "role");
        //        return result;
        //    });
        //});
    })
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new()
        {
            ValidIssuer = jwtConfig.Issuer,
            ValidAudience = jwtConfig.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtConfig.Key)),
        };
    })
    .AddCookie(options =>
    {
        // Cookie settings
        options.Cookie.HttpOnly = true;
        //options.Cookie.Expiration 
        options.ExpireTimeSpan = TimeSpan.FromHours(1);
        options.LoginPath = "/Identity/Account/Login";
        options.LogoutPath = "/Identity/Account/Logout";
        options.AccessDeniedPath = "/Identity/Account/AccessDenied";
        options.SlidingExpiration = true;
        //options.ReturnUrlParameter = "";

    })
    .AddOpenAuthProviders(config);

#endregion

// Add services to the container.
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDatabaseDeveloperPageExceptionFilter();
}

// API endpoint explorer/reference
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = Strings.AssemblyName, Version = "v1" });
});

#region Configuration

builder.Services.Configure<AuthMessageSenderOptions>(config.GetSection("Keys"));
builder.Services.Configure<EntityMemoryCacheConfig>(config.GetSection("Cache"));
builder.Services.Configure<JwtAuthConfig>(config.GetSection("Jwt"));
builder.Services.Configure<LeafletMapConfig>(config.GetSection("Map"));
builder.Services.Configure<MySqlResiliencyOptions>(config.GetSection("Database"));

#endregion

#region Database Contexts

// Register data contexts, factories, and pools
builder.Services.AddDbContextFactory<ControllerDbContext>(options =>
    options.GetDbContextOptions(connectionString, serverVersion, Strings.AssemblyName, resiliencyConfig), ServiceLifetime.Singleton);
builder.Services.AddDbContextFactory<MapDbContext>(options =>
    options.GetDbContextOptions(connectionString, serverVersion, Strings.AssemblyName, resiliencyConfig), ServiceLifetime.Singleton);
builder.Services.AddDbContextPool<ControllerDbContext>(options =>
    options.GetDbContextOptions(connectionString, serverVersion, Strings.AssemblyName, resiliencyConfig), resiliencyConfig.MaximumPoolSize);
builder.Services.AddDbContextPool<MapDbContext>(options =>
    options.GetDbContextOptions(connectionString, serverVersion, Strings.AssemblyName, resiliencyConfig), resiliencyConfig.MaximumPoolSize);

var sqliteConnectionString = $"Data Source={Strings.PluginsDatabasePath}";
builder.Services.AddDbContextFactory<PluginDbContext>(options =>
    options.UseSqlite(sqliteConnectionString), ServiceLifetime.Singleton);

//builder.Services.AddDbContext<ControllerDbContext>(options =>
//    options.GetDbContextOptions(connectionString, serverVersion, Strings.AssemblyName, resiliencyConfig));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork<ControllerDbContext>>();
//builder.Services.AddScoped<IUnitOfWork, UnitOfWork<MapDbContext>>();

#endregion

#region Services

builder.Services.AddHttpContextAccessor();
builder.Services.AddRazorPages(); // <- Required for plugins to render Razor pages
builder.Services.AddSingleton<IAssignmentControllerService, AssignmentControllerService>();
builder.Services.AddSingleton<IGeofenceControllerService, GeofenceControllerService>();
builder.Services.AddSingleton<IIvListControllerService, IvListControllerService>();

builder.Services.AddSingleton<IMemoryCacheHostedService>(factory =>
{
    using var scope = factory.CreateScope();
    var serviceProvider = scope.ServiceProvider;
    var memCacheOptions = serviceProvider.GetService<IOptions<EntityMemoryCacheConfig>>();
    var memCacheConfig = memCacheOptions?.Value ?? new();
    var memCache = new GenericMemoryCacheHostedService(
        GenericLoggerFactory.CreateLogger<IMemoryCacheHostedService>(),
        Options.Create(memCacheConfig)
    ); ;
    return memCache;
});
builder.Services.AddSingleton<IWebhookControllerService, WebhookControllerService>();
builder.Services.AddSingleton<ITimeZoneService, TimeZoneService>();
builder.Services.AddSingleton<IJobControllerService, JobControllerService>();
builder.Services.AddTransient<IEmailSender, SendGridEmailSender>();
//builder.Services.AddSingleton<IRouteGenerator, RouteGenerator>();
builder.Services.AddTransient<IRouteCalculator, RouteCalculator>();

builder.Services.AddScoped<IApiKeyManagerService, ApiKeyManagerService>();

builder.Services.AddGrpc(options =>
{
    options.IgnoreUnknownServices = true;
    //options.EnableDetailedErrors = true;
    options.ResponseCompressionLevel = System.IO.Compression.CompressionLevel.Optimal;
});

builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache();

#endregion

#region Hosted Services

builder.Services.AddHostedService<GenericMemoryCacheHostedService>();

#endregion

#region Plugins

// Register plugin host handlers
var uiHost = new UiHost();
var databaseHost = new DatabaseHost(connectionString);
var loggingHost = new LoggingHost();
var fileStorageHost = new FileStorageHost(Strings.PluginsFolder);
var configurationProviderHost = new ConfigurationHost(Strings.PluginsFolder);
var geofenceServiceHost = new GeofenceServiceHost(connectionString);
var eventAggregatorHost = new EventAggregatorHost();
eventAggregatorHost.Subscribe(new PluginObserver());

builder.Services.AddSingleton<IConfigurationHost>(configurationProviderHost);
builder.Services.AddSingleton<IDatabaseHost>(databaseHost);
builder.Services.AddSingleton<IFileStorageHost>(fileStorageHost);
builder.Services.AddSingleton<ILocalizationHost>(Translator.Instance);
builder.Services.AddSingleton<ILoggingHost>(loggingHost);
builder.Services.AddSingleton<IUiHost>(uiHost);
builder.Services.AddSingleton<IGeofenceServiceHost>(geofenceServiceHost);
builder.Services.AddSingleton<IRoutingHost, RouteGenerator>();
builder.Services.AddSingleton<IEventAggregatorHost>(eventAggregatorHost);
builder.Services.AddScoped<IPublisher, PluginPublisher>();


// TODO: Do not build service provider from collection manually, leave it up to DI - https://andrewlock.net/access-services-inside-options-and-startup-using-configureoptions/
var serviceProvider = builder.Services.BuildServiceProvider();

// Seed default user and roles
await SeedDefaultDataAsync(serviceProvider);

var routeHost = serviceProvider.GetService<IRoutingHost>();
var jobControllerService = serviceProvider.GetService<IJobControllerService>();
builder.Services.AddSingleton<IJobControllerServiceHost>(jobControllerService);
builder.Services.AddSingleton<IInstanceServiceHost>(jobControllerService);
// Load all devices
jobControllerService.LoadDevices();

// TODO: Use builder.Services registered instead of 'sharedServiceHosts' - Fix issue with IDbContextFactory and eventually ILogger<T> parameters (just make static and use logger factory instead)
var sharedServiceHosts = new Dictionary<Type, object>
{
    { typeof(ILoggingHost), loggingHost },
    { typeof(IJobControllerServiceHost), jobControllerService },
    { typeof(IDatabaseHost), databaseHost },
    { typeof(ILocalizationHost), Translator.Instance },
    { typeof(IUiHost), uiHost },
    { typeof(IFileStorageHost), fileStorageHost },
    { typeof(IConfigurationHost), configurationProviderHost },
    { typeof(IGeofenceServiceHost), geofenceServiceHost },
    { typeof(IInstanceServiceHost), jobControllerService },
    { typeof(IRoutingHost), routeHost },
    { typeof(IEventAggregatorHost), eventAggregatorHost },
};

// TODO: Retrieve and pass/set api keys upon change
var apiKeys = new List<ApiKey>();
var controllerContext = serviceProvider.GetService<IDbContextFactory<ControllerDbContext>>();
if (controllerContext != null)
{
    using var context = controllerContext.CreateDbContext();
    apiKeys = context.ApiKeys.ToList();
}

// Load plugin states from SQLite database
var pluginStates = new List<Plugin>();
var pluginContext = serviceProvider.GetService<IDbContextFactory<PluginDbContext>>();
if (pluginContext != null)
{
    using var context = pluginContext.CreateDbContext();
    pluginStates = context.Plugins.ToList();
}

// Instantiate 'IPluginManager' singleton with configurable options
var pluginManager = PluginManager.InstanceWithOptions(new PluginManagerOptions
{
    RootPluginsDirectory = Strings.PluginsFolder,
    Configuration = config,
    Services = builder.Services,
    SharedServiceHosts = sharedServiceHosts,
});
pluginManager.PluginHostAdded += OnPluginHostAdded;
pluginManager.PluginHostRemoved += OnPluginHostRemoved;
pluginManager.PluginHostStateChanged += OnPluginHostStateChanged;

// Find plugins, register plugin services, load plugin assemblies,
// call OnLoad callback and register with 'IPluginManager' cache
await pluginManager.LoadPluginsAsync(builder.Services, builder.Environment, apiKeys); //pluginStates);

// Start the job controller service after all plugins have loaded. (TODO: Add PluginsLoadedComplete event?)
// This is so all custom IJobController's provided via plugins have been registered.
jobControllerService.Start();

#endregion

#region App Builder

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseMigrationsEndPoint();

    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    //app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// TODO: app.UseMiddleware<UnhandledExceptionMiddleware>();
if (jwtConfig.Enabled)
{
    // Controls whether to protect the gRPC endpoints with JWT
    app.UseWhen(
        context => context.Request.ContentType == "application/grpc",
        appBuilder => appBuilder.UseMiddleware<JwtValidatorMiddleware>()
    );
}

// Call 'Configure' method in plugins
pluginManager.Configure(app);

//app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// User authentication
app.UseAuthentication();
app.UseCookiePolicy(new CookiePolicyOptions
{
    // Determine whether user consent for non-essential 
    // cookies is needed for a given request
    CheckConsentNeeded = context => true,
    // https://stackoverflow.com/a/64874175
    MinimumSameSitePolicy = SameSiteMode.Lax,
});
app.UseAuthorization();
//app.UseSession();

app.MapGrpcService<ProtoPayloadServerService>();
app.MapGrpcService<TrainerInfoServerService>();
app.MapGrpcService<WebhookEndpointServerService>();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();

#endregion

#region Plugin Callback/Event Handlers

async void OnPluginHostAdded(object? sender, PluginHostAddedEventArgs e)
{
    logger.LogInformation($"Plugin added successfully: {e.PluginHost.Plugin.Name}");
    await AddOrUpdatePluginState(e.PluginHost);
}

async void OnPluginHostRemoved(object? sender, PluginHostRemovedEventArgs e)
{
    logger.LogInformation($"Plugin removed successfully: {e.PluginHost.Plugin.Name}");
    using (var scope = serviceProvider.CreateScope())
    {
        var services = scope.ServiceProvider;
        var factory = services.GetRequiredService<IDbContextFactory<PluginDbContext>>();
        using var context = await factory.CreateDbContextAsync();

        // Get cached plugin state from database
        var dbPlugin = await context.Plugins.FindAsync(e.PluginHost.Plugin.Name);
        if (dbPlugin == null)
            return;

        // Plugin host is cached in database, set previous plugin state,
        // otherwise set state from param
        if (dbPlugin.State != e.PluginHost.State)
        {
            //e.PluginHost.SetState(dbPlugin.State);
            dbPlugin.State = e.PluginHost.State;
            context.Plugins.Remove(dbPlugin);

            // Save plugin host state to database
            await context.SaveChangesAsync();
        }
    }
}

async void OnPluginHostStateChanged(object? sender, PluginHostStateChangedEventArgs e)
{
    logger.LogInformation($"Plugin state changed: {e.PluginHost.Plugin.Name} from {e.PreviousState} to {e.PluginHost.State}");
    await AddOrUpdatePluginState(e.PluginHost);
}

#endregion

#region Helpers

async Task SeedDefaultDataAsync(IServiceProvider serviceProvider)
{
    using var scope = serviceProvider.CreateScope();
    var services = scope.ServiceProvider;
    try
    {
        // Migrate database to latest migration automatically if enabled
        if (config.GetValue<bool>("AutomaticMigrations"))
        {
            // Migrate the UserIdentity tables
            await serviceProvider.MigrateDatabaseAsync<UserIdentityContext>();

            // Migrate the device controller tables
            await serviceProvider.MigrateDatabaseAsync<ControllerDbContext>();
        }

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        // Seed default user roles
        await UserIdentityContextSeed.SeedRolesAsync(roleManager);

        // Seed default SuperAdmin user
        await UserIdentityContextSeed.SeedSuperAdminAsync(userManager);

        var assignmentController = services.GetRequiredService<IAssignmentControllerService>();

        // Start assignment controller service
        assignmentController.Start();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

IdentityOptions GetDefaultIdentityOptions()
{
    var options = new IdentityOptions
    {
        Lockout = new LockoutOptions
        {
            AllowedForNewUsers = true,
            MaxFailedAccessAttempts = 5,
            DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15),
        },
        Password = new PasswordOptions
        {
            RequireDigit = true,
            RequiredLength = 8,
            RequiredUniqueChars = 1,
            RequireLowercase = true,
            RequireUppercase = true,
            RequireNonAlphanumeric = true,
        },
        SignIn = new SignInOptions
        {
            RequireConfirmedAccount = true,
            RequireConfirmedEmail = true,
            RequireConfirmedPhoneNumber = false,
        },
        User = new UserOptions
        {
            AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+",
            RequireUniqueEmail = true,
        },
        //options.Stores.ProtectPersonalData = true;
    };
    return options;
}

async Task AddOrUpdatePluginState(IPluginHost pluginHost)
{
    if (serviceProvider == null)
    {
        throw new NullReferenceException(nameof(serviceProvider));
    }

    using (var scope = serviceProvider.CreateScope())
    {
        var services = scope.ServiceProvider;
        var factory = services.GetRequiredService<IDbContextFactory<PluginDbContext>>();
        using var context = await factory.CreateDbContextAsync();

        // Get cached plugin state from database
        var dbPlugin = await context.Plugins.FindAsync(pluginHost.Plugin.Name);
        if (dbPlugin == null)
        {
            // Plugin host is not cached in database. Set current state to plugin
            // host and add insert into database
            await context.Plugins.AddAsync(new Plugin
            {
                Name = pluginHost.Plugin.Name,
                FullPath = pluginHost.Assembly.AssemblyFullPath,
                State = pluginHost.State,
            });
        }
        else
        {
            // Plugin host is cached in database, set previous plugin state,
            // otherwise set state from param
            if (dbPlugin.State != pluginHost.State)
            {
                //e.PluginHost.SetState(dbPlugin.State);
                dbPlugin.State = pluginHost.State;

                // Update plugin host state
                context.Plugins.Update(dbPlugin);
            }
        }
        await context.SaveChangesAsync();
    }
}

#endregion