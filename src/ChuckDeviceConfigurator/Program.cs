using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
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
using ChuckDeviceConfigurator.Services.Plugins.Hosts;
using ChuckDeviceConfigurator.Services.Routing;
using ChuckDeviceConfigurator.Services.Rpc;
using ChuckDeviceConfigurator.Services.TimeZone;
using ChuckDeviceConfigurator.Services.Webhooks;
using ChuckDeviceController.Configuration;
using ChuckDeviceController.Data.Contexts;
using ControllerContext = ChuckDeviceController.Data.Contexts.ControllerContext;
using ChuckDeviceController.Data.Entities;
using ChuckDeviceController.Extensions.Data;
using ChuckDeviceController.Plugin;
using ChuckDeviceController.PluginManager;


// TODO: Create 'CopyPlugin.sh' script for plugins to execute post build event (.dlls other than CDC libs, Views, wwwroot, .deps.json file, etc)
// TODO: Show top navbar on mobile when sidebar is closed?

var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
var config = Config.LoadConfig(args, env);
if (config.Providers.Count() == 2)
{
    // Only environment variables and command line providers added,
    // failed to load config provider.
    Environment.FailFast($"Failed to find or load configuration file, exiting...");
}

var logger = new Logger<Program>(LoggerFactory.Create(x => x.AddConsole()));

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
// Need to call at startup so time gets set now and not when first visit to dashboard
logger.LogDebug($"Uptime: {Strings.Uptime}");

// Add services to the container.
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseConfiguration(config);

#region Logger Filtering

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
    //configure.AddFilter("Microsoft.EntityFrameworkCore.Model.Validation", LogLevel.Error);
    configure.AddFilter("Microsoft.EntityFrameworkCore.Update", LogLevel.None);
    configure.AddFilter("Microsoft.AspNetCore.Diagnostics.DeveloperExceptionPageMiddleware", LogLevel.None);
});

#endregion

#region User Identity

// https://codewithmukesh.com/blog/user-management-in-aspnet-core-mvc/
builder.Services.AddDbContext<UserIdentityContext>(options =>
    options.GetDbContextOptions(connectionString, serverVersion, Strings.AssemblyName), ServiceLifetime.Transient);

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options => GetDefaultIdentityOptions())
    .AddDefaultUI()
    .AddEntityFrameworkStores<UserIdentityContext>()
    .AddDefaultTokenProviders();

builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = long.MaxValue;
});
//builder.Services.AddDistributedMemoryCache();

// Register external 3rd party authentication providers if configured
builder.Services
    .AddAuthentication()
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
    .AddOpenAuthProviders(builder.Configuration);

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

#region Database Contexts

builder.Services.AddDbContextFactory<ControllerContext>(options =>
    options.GetDbContextOptions(connectionString, serverVersion, Strings.AssemblyName), ServiceLifetime.Singleton);
builder.Services.AddDbContextFactory<MapContext>(options =>
    options.GetDbContextOptions(connectionString, serverVersion, Strings.AssemblyName), ServiceLifetime.Singleton);
builder.Services.AddDbContext<ControllerContext>(options =>
    options.GetDbContextOptions(connectionString, serverVersion, Strings.AssemblyName), ServiceLifetime.Scoped);
builder.Services.AddDbContext<MapContext>(options =>
    options.GetDbContextOptions(connectionString, serverVersion, Strings.AssemblyName), ServiceLifetime.Scoped);

#endregion

#region Host Services

builder.Services.AddSingleton<IAssignmentControllerService, AssignmentControllerService>();
builder.Services.AddSingleton<IGeofenceControllerService, GeofenceControllerService>();
builder.Services.AddSingleton<IIvListControllerService, IvListControllerService>();
builder.Services.AddSingleton<IWebhookControllerService, WebhookControllerService>();
builder.Services.AddSingleton<ITimeZoneService, TimeZoneService>();
// TODO: Remove extra service registration of 'JobControllerService' or confirm there are no issues and two instances are not created
builder.Services.AddSingleton<IJobControllerService, JobControllerService>();
builder.Services.AddTransient<IEmailSender, SendGridEmailSender>();
builder.Services.AddSingleton<IRouteGenerator, RouteGenerator>();
builder.Services.AddTransient<IRouteCalculator, RouteCalculator>();

builder.Services.Configure<AuthMessageSenderOptions>(builder.Configuration.GetSection("Keys"));

builder.Services.AddGrpc(options =>
{
    options.IgnoreUnknownServices = true;
    options.EnableDetailedErrors = true;
    options.ResponseCompressionLevel = System.IO.Compression.CompressionLevel.Optimal;
});

#endregion

#region Plugins

// Register plugin host handlers
/*
var jobControllerService = new JobControllerService(
    null,
    null,
    TimeZoneService.Instance,
    null,
    null,
    null,
    null,
    null
);
*/
var uiHost = new UiHost();
var databaseHost = new DatabaseHost(connectionString);
var loggingHost = new LoggingHost();
var fileStorageHost = new FileStorageHost(Strings.PluginsFolder);
var configurationProviderHost = new ConfigurationHost(Strings.PluginsFolder);
//var instanceServiceHost = new InstanceServiceHost(connectionString);
var geofenceServiceHost = new GeofenceServiceHost(connectionString);
builder.Services.AddSingleton<IConfigurationHost>(configurationProviderHost);
builder.Services.AddSingleton<IDatabaseHost>(databaseHost);
builder.Services.AddSingleton<IFileStorageHost>(fileStorageHost);
builder.Services.AddSingleton<ILocalizationHost>(Translator.Instance);
builder.Services.AddSingleton<ILoggingHost>(loggingHost);
builder.Services.AddSingleton<IUiHost>(uiHost);
builder.Services.AddSingleton<IGeofenceServiceHost>(geofenceServiceHost);

builder.Services.AddHttpContextAccessor();

IJobControllerService? jobControllerService = null;
var scopeFactory = builder.Services
                            .BuildServiceProvider()
                            .GetRequiredService<IServiceScopeFactory>();

using var scope = scopeFactory.CreateScope();
var provider = scope.ServiceProvider;
jobControllerService = provider.GetRequiredService<IJobControllerService>();
jobControllerService.Start();

builder.Services.AddSingleton<IJobControllerServiceHost>(jobControllerService);
//builder.Services.AddSingleton<IJobControllerService>(jobControllerService);
builder.Services.AddSingleton<IInstanceServiceHost>(jobControllerService);

// TODO: Use builder.Services registered instead of 'sharedServiceHosts'
var sharedServiceHosts = new Dictionary<Type, object>
{
    { typeof(ILoggingHost), loggingHost },
    // TODO: Break out JobControllerService to fix shared service host instance injection for plugins
    { typeof(IJobControllerServiceHost), jobControllerService },
    { typeof(IDatabaseHost), databaseHost },
    { typeof(ILocalizationHost), Translator.Instance },
    { typeof(IUiHost), uiHost },
    { typeof(IFileStorageHost), fileStorageHost },
    { typeof(IConfigurationHost), configurationProviderHost },
    { typeof(IGeofenceServiceHost), geofenceServiceHost },
    { typeof(IInstanceServiceHost), jobControllerService },
};

// Instantiate 'IPluginManager' singleton with configurable options
var pluginManager = PluginManager.InstanceWithOptions(new PluginManagerOptions
{
    Configuration = builder.Configuration,
    RootPluginsDirectory = Strings.PluginsFolder,
    Services = builder.Services,
    SharedServiceHosts = sharedServiceHosts,
});

// Find plugins, register plugin services, load plugin assemblies,
// call OnLoad callback and register with 'IPluginManager' cache
await pluginManager.LoadPluginsAsync(builder.Services, builder.Environment);

//builder.Services.AddSingleton<IPluginManagerOptions>(pluginManager.Options);
//builder.Services.AddSingleton<IPluginManager>(pluginManager);

#endregion

#region App Builder

var app = builder.Build();

pluginManager.PluginHostAdded += OnPluginHostAdded;
pluginManager.PluginHostRemoved += OnPluginHostRemoved;
pluginManager.PluginHostStateChanged += OnPluginHostStateChanged;

// Seed default user and roles
await SeedDefaultDataAsync(app.Services);

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
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
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
    CheckConsentNeeded = context => true, // TODO: Make configurable
    // https://stackoverflow.com/a/64874175
    MinimumSameSitePolicy = SameSiteMode.Lax,
});
app.UseAuthorization();

// gRPC listener server services
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

void OnPluginHostAdded(object? sender, PluginHostAddedEventArgs e)
{
    logger.LogInformation($"Plugin added successfully: {e.PluginHost.Plugin.Name}");
}

void OnPluginHostRemoved(object? sender, PluginHostRemovedEventArgs e)
{
    logger.LogInformation($"Plugin removed successfully: {e.PluginHost.Plugin.Name}");
}

async void OnPluginHostStateChanged(object? sender, PluginHostStateChangedEventArgs e)
{
    var serviceProvider = app.Services;
    using (var scope = serviceProvider.CreateScope())
    {
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<ControllerContext>();
        var canUpdate = false;

        // Get cached plugin state from database
        var dbPlugin = await context.Plugins.FindAsync(e.PluginHost.Plugin.Name);
        if (dbPlugin != null)
        {
            // Plugin host is cached in database, set previous plugin state,
            // otherwise set state from param
            if (dbPlugin.State != e.PluginHost.State)
            {
                //e.PluginHost.SetState(dbPlugin.State);
                dbPlugin.State = e.PluginHost.State;
                context.Plugins.Update(dbPlugin);

                canUpdate = true;
            }
        }
        else
        {
            // Plugin host is not cached in database. Set current state to plugin
            // host and add insert into database
            dbPlugin = new Plugin
            {
                Name = e.PluginHost.Plugin.Name,
                State = e.PluginHost.State,
            };
            await context.Plugins.AddAsync(dbPlugin);

            canUpdate = true;
        }

        if (canUpdate)
        {
            // Save plugin host state to database
            await context.SaveChangesAsync();
        }
    }
}

#endregion

#region Helpers

async Task SeedDefaultDataAsync(IServiceProvider serviceProvider)
{
    using (var scope = serviceProvider.CreateScope())
    {
        var services = scope.ServiceProvider;
        var loggerFactory = services.GetRequiredService<ILoggerFactory>();
        try
        {
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var jobController = services.GetRequiredService<IJobControllerService>();
            var assignmentController = services.GetRequiredService<IAssignmentControllerService>();

            // Migrate database to latest migration automatically if enabled
            if (config.GetValue<bool>("AutomaticMigrations"))
            {
                // Migrate the UserIdentity tables
                await serviceProvider.MigrateDatabaseAsync<UserIdentityContext>();

                // Migrate the device controller tables
                await serviceProvider.MigrateDatabaseAsync<ControllerContext>();
            }

            // TODO: Add database meta or something to determine if default entities have been seeded

            // Start job controller service
            jobController.Start();

            // Start assignment controller service
            assignmentController.Start();
            // Seed default user roles
            await UserIdentityContextSeed.SeedRolesAsync(roleManager);

            // Seed default SuperAdmin user
            await UserIdentityContextSeed.SeedSuperAdminAsync(userManager);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database.");
        }
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
            //RequireConfirmedPhoneNumber = true,
        },
        //options.User.RequireUniqueEmail = true;
        //options.Stores.ProtectPersonalData = true;
        //options.ClaimsIdentity.EmailClaimType
    };
    return options;
}

#endregion