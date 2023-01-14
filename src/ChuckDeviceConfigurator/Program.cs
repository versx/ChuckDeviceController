using System.Globalization;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Localization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MySqlConnector;

using ChuckDeviceConfigurator;
using ChuckDeviceConfigurator.Data;
using ChuckDeviceConfigurator.Extensions;
using ChuckDeviceConfigurator.Localization;
using ChuckDeviceConfigurator.HostedServices;
using ChuckDeviceConfigurator.Middleware;
using ChuckDeviceConfigurator.Services.Assignments;
using ChuckDeviceConfigurator.Services.Geofences;
using ChuckDeviceConfigurator.Services.Icons;
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
using ChuckDeviceConfigurator.Utilities;
using ChuckDeviceController.Authorization.Jwt.Middleware;
using ChuckDeviceController.Caching.Memory;
using ChuckDeviceController.Configuration;
using ChuckDeviceController.Data.Contexts;
using ChuckDeviceController.Data.Entities;
using ChuckDeviceController.Data.Factories;
using ChuckDeviceController.Data.Repositories;
using ChuckDeviceController.Data.Repositories.Dapper;
using ChuckDeviceController.Extensions.Data;
using ChuckDeviceController.Logging;
using ChuckDeviceController.Plugin;
using ChuckDeviceController.Plugin.EventBus;
using ChuckDeviceController.Plugin.EventBus.Observer;
using ChuckDeviceController.PluginManager;
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
config.Bind("UserIdentity::UserAccounts", identityConfig);

// SendGrid email sender service options
var emailConfig = new AuthMessageSenderOptions();
config.Bind("EmailService", emailConfig);

var locale = config.GetValue<string>("Locale") ?? "en";

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

CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(locale);
CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(locale);
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo(locale);

builder.Services.AddSingleton<IStringLocalizerFactory, JsonStringLocalizerFactory>();
builder.Services.AddSingleton<IStringLocalizer, JsonStringLocalizer>();
builder.Services.AddLocalization();

// API endpoint explorer/reference
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = Strings.AssemblyName, Version = "v1" });
});

#region Configuration

builder.Services.Configure<AuthMessageSenderOptions>(config.GetSection("EmailService"));
builder.Services.Configure<EntityMemoryCacheConfig>(config.GetSection("Cache"));
builder.Services.Configure<JwtAuthConfig>(config.GetSection("Jwt"));
builder.Services.Configure<LeafletMapConfig>(config.GetSection("Map"));
builder.Services.Configure<LoginLimitConfig>(config.GetSection("UserIdentity::LoginLimit"));
builder.Services.Configure<MySqlResiliencyOptions>(config.GetSection("Database"));

#endregion

#region Database Contexts

// Register data contexts, factories, and pools
builder.Services.AddDbContextPool<ControllerDbContext>(options =>
    options.GetDbContextOptions(connectionString, serverVersion, Strings.AssemblyName, resiliencyConfig), resiliencyConfig.MaximumPoolSize);

var sqliteConnectionString = $"Data Source={Strings.PluginsDatabasePath}";
builder.Services.AddDbContextFactory<PluginDbContext>(options =>
    options.UseSqlite(sqliteConnectionString), ServiceLifetime.Singleton);

builder.Services.AddScoped<IUnitOfWork, UnitOfWork<ControllerDbContext>>();
builder.Services.AddSingleton<IDapperUnitOfWork, DapperUnitOfWork>();

builder.Services.AddScoped<MySqlConnection>(options =>
{
    var connection = new MySqlConnection(connectionString);
    try
    {
        //Task.Run(connection.OpenAsync).Wait();
        connection.Open();
    }
    catch (Exception ex)
    {
        logger.LogError("Failed to open connection to MySQL server: {Message}", ex.InnerException?.Message ?? ex.Message);
    }
    return connection;
});
builder.Services.AddSingleton<IMySqlConnectionFactory>(sp =>
{
    var factory = new MySqlConnectionFactory(config);
    return factory;
});

#endregion

#region Services

builder.Services.AddHttpContextAccessor();
builder.Services
    .AddControllersWithViews()
    .AddRazorRuntimeCompilation();
builder.Services
    .AddRazorPages() // <- Required for plugins to render Razor pages
    .AddRazorRuntimeCompilation();
builder.Services.AddSingleton<IAssignmentControllerService, AssignmentControllerService>();
builder.Services.AddSingleton<IGeofenceControllerService, GeofenceControllerService>();
builder.Services.AddSingleton<IIvListControllerService, IvListControllerService>();

builder.Services.AddSingleton<IMemoryCacheService, GenericMemoryCacheService>();
builder.Services.AddSingleton<IWebhookControllerService, WebhookControllerService>();
builder.Services.AddSingleton<ITimeZoneService, TimeZoneService>();
builder.Services.AddSingleton<IJobControllerService, JobControllerService>();
if (emailConfig.Enabled)
{
    builder.Services.AddTransient<IEmailSender, SendGridEmailSender>();
}
//builder.Services.AddSingleton<IRouteGenerator, RouteGenerator>();
builder.Services.AddTransient<IRouteCalculator, RouteCalculator>();

builder.Services.AddScoped<IApiKeyManagerService, ApiKeyManagerService>();
builder.Services.AddSingleton<ILoginLimiter, LoginLimiter>();

// Reference: https://learn.microsoft.com/en-us/aspnet/core/mvc/views/working-with-forms?view=aspnetcore-7.0#checkbox-hidden-input-rendering
builder.Services.Configure<MvcViewOptions>(options =>
    options.HtmlHelperOptions.CheckBoxHiddenInputRenderMode =
        CheckBoxHiddenInputRenderMode.EndOfForm);

builder.Services.AddGrpc(options =>
{
    options.EnableDetailedErrors = false;
    options.IgnoreUnknownServices = true;
    options.ResponseCompressionLevel = System.IO.Compression.CompressionLevel.Optimal;
});

builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache();

#endregion

#region Hosted Services

// Register available hosted services
if (config.GetValue<bool>("AccountStatusService", false))
{
    builder.Services.AddHostedService<AccountStatusHostedService>();
}

#endregion

#region Plugins

// Register plugin host handlers
builder.Services.AddSingleton<IConfigurationHost, ConfigurationHost>();
builder.Services.AddSingleton<IDatabaseHost, DatabaseHost>();
builder.Services.AddSingleton<IFileStorageHost, FileStorageHost>();
builder.Services.AddSingleton<ILocalizationHost>(Translator.Instance);
builder.Services.AddSingleton<ILoggingHost, LoggingHost>();
builder.Services.AddSingleton<IUiHost, UiHost>();
builder.Services.AddSingleton<IGeofenceServiceHost, GeofenceServiceHost>();
builder.Services.AddSingleton<IRoutingHost, RouteGenerator>();
builder.Services.AddSingleton<IEventAggregatorHost, EventAggregatorHost>();
builder.Services.AddSingleton<IMemoryCacheHost, MemoryCacheHost>();
builder.Services.AddSingleton<IUIconsHost>(UIconsService.Instance);
builder.Services.AddScoped<IPublisher, PluginPublisher>();
builder.Services.AddScoped<IAuthorizeHost, AuthorizeHost>();


// TODO: Do not build service provider from collection manually, leave it up to DI - https://andrewlock.net/access-services-inside-options-and-startup-using-configureoptions/
var serviceProvider = builder.Services.BuildServiceProvider();

// Seed default user and roles
await SeedDefaultDataAsync(serviceProvider);

var authHost = serviceProvider.GetRequiredService<IAuthorizeHost>();
var configurationHost = serviceProvider.GetRequiredService<IConfigurationHost>();
var databaseHost = serviceProvider.GetRequiredService<IDatabaseHost>();
var eventAggregatorHost = serviceProvider.GetRequiredService<IEventAggregatorHost>();
var fileStorageHost = serviceProvider.GetRequiredService<IFileStorageHost>();
var geofenceServiceHost = serviceProvider.GetRequiredService<IGeofenceServiceHost>();
var uiHost = serviceProvider.GetRequiredService<IUiHost>();
var jobControllerService = serviceProvider.GetRequiredService<IJobControllerService>();
var loggingHost = serviceProvider.GetRequiredService<ILoggingHost>();
var memCacheHost = serviceProvider.GetRequiredService<IMemoryCacheHost>();
var routeGeneratorHost = serviceProvider.GetRequiredService<IRoutingHost>();
builder.Services.AddSingleton<IJobControllerServiceHost>(jobControllerService);
builder.Services.AddSingleton<IInstanceServiceHost>(jobControllerService);

eventAggregatorHost.Subscribe(new PluginObserver());

// Load all devices
jobControllerService.LoadDevices(serviceProvider);

var sharedServiceHosts = new Dictionary<Type, object>
{
    { typeof(IAuthorizeHost), authHost },
    { typeof(ILoggingHost), loggingHost },
    { typeof(IJobControllerServiceHost), jobControllerService },
    { typeof(IDatabaseHost), databaseHost },
    { typeof(ILocalizationHost), Translator.Instance },
    { typeof(IUiHost), uiHost },
    { typeof(IFileStorageHost), fileStorageHost },
    { typeof(IConfigurationHost), configurationHost },
    { typeof(IGeofenceServiceHost), geofenceServiceHost },
    { typeof(IInstanceServiceHost), jobControllerService },
    { typeof(IRoutingHost), routeGeneratorHost },
    { typeof(IEventAggregatorHost), eventAggregatorHost },
    { typeof(IMemoryCacheHost), memCacheHost },
    { typeof(IUIconsHost), UIconsService.Instance },
};

var getApiKeysFunc = new Func<List<ApiKey>>(() =>
{
    var factory = new MySqlConnectionFactory(connectionString);
    var apiKeysRepository = new ApiKeyRepository(factory);
    var apiKeys = apiKeysRepository.FindAllAsync().Result;
    return apiKeys.ToList();
});

// Load plugin states from SQLite database
var getPluginsFunc = new Func<List<Plugin>>(() =>
{
    var controllerContext = serviceProvider.GetRequiredService<IDbContextFactory<PluginDbContext>>();
    using var context = controllerContext.CreateDbContext();
    var plugins = context.Plugins.ToList();
    return plugins;
});

// Instantiate 'IPluginManager' singleton with configurable options
var pluginManager = PluginManager.InstanceWithOptions(new PluginManagerOptions
{
    RootPluginsDirectory = Strings.PluginsFolder,
    Configuration = config,
    Services = builder.Services,
    ServiceProvider = serviceProvider,
    SharedServiceHosts = sharedServiceHosts,
});
pluginManager.PluginHostAdded += OnPluginHostAdded;
pluginManager.PluginHostRemoved += OnPluginHostRemoved;
pluginManager.PluginHostStateChanged += OnPluginHostStateChanged;

// Find plugins, register plugin services, load plugin assemblies,
// call OnLoad callback and register with 'IPluginManager' cache
await pluginManager.LoadPluginsAsync(builder.Services, builder.Environment, getApiKeysFunc, getPluginsFunc, serviceProvider);

// Start the job controller service after all plugins have loaded. (TODO: Add PluginsLoadedComplete event?)
// This is so all custom IJobController's provided via plugins have been registered.
jobControllerService.Start();

#endregion

#region App Builder

var app = builder.Build();

//await SeedDefaultDataAsync(app.Services);

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

app.UseMiddleware<PageLoadTimeMiddleware>();

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

    using var scope = serviceProvider.CreateScope();
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