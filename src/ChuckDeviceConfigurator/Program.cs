using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

using ChuckDeviceConfigurator;
using ChuckDeviceConfigurator.Data;
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
using ChuckDeviceController.Data.Extensions;
using ChuckDeviceController.PluginManager;
using ChuckDeviceController.PluginManager.FileProviders;
using ChuckDeviceController.PluginManager.Mvc.Extensions;
using ChuckDeviceController.PluginManager.Mvc.Razor;
using ChuckDeviceController.Plugins;


// TODO: Show top navbar on mobile when sidebar is closed?
// TODO: Create separate gRPC server service for all gRPC calls


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
{
    options.UseMySql(connectionString, serverVersion, opt =>
    {

        //opt.MigrationsHistoryTable("migrations");
        opt.MigrationsAssembly(Strings.AssemblyName);
    });
}, ServiceLifetime.Transient);

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
})
    .AddEntityFrameworkStores<UserIdentityContext>()
    .AddDefaultUI()
    .AddDefaultTokenProviders();

builder.Services.Configure<IdentityOptions>(options => GetDefaultIdentityOptions());

builder.Services.ConfigureApplicationCookie(options =>
{
    // Cookie settings
    options.Cookie.HttpOnly = true;
    //options.Cookie.Expiration 

    options.ExpireTimeSpan = TimeSpan.FromHours(1);
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.SlidingExpiration = true;
    //options.ReturnUrlParameter=""
});

/*
// Set policy that users need to be authenticated to access
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
*/

// Register external 3rd party authentication providers if configured
var auth = builder.Services.AddAuthentication();
RegisterAuthProviders(auth);

#endregion

// Add services to the container.
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDatabaseDeveloperPageExceptionFilter();
}
var mvcBuilder = builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();
builder.Services.AddRazorPages();

// API endpoint explorer/reference
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = Strings.AssemblyName, Version = "v1" });
});

#region Database Contexts

builder.Services.AddDbContextFactory<ControllerContext>(options =>
         options.UseMySql(connectionString, serverVersion, opt => opt.MigrationsAssembly(Strings.AssemblyName)), ServiceLifetime.Singleton);
builder.Services.AddDbContextFactory<MapContext>(options =>
         options.UseMySql(connectionString, serverVersion, opt => opt.MigrationsAssembly(Strings.AssemblyName)), ServiceLifetime.Singleton);
builder.Services.AddDbContext<ControllerContext>(options =>
         options.UseMySql(connectionString, serverVersion, opt => opt.MigrationsAssembly(Strings.AssemblyName)), ServiceLifetime.Scoped);
builder.Services.AddDbContext<MapContext>(options =>
         options.UseMySql(connectionString, serverVersion, opt => opt.MigrationsAssembly(Strings.AssemblyName)), ServiceLifetime.Scoped);

#endregion

#region Services

builder.Services.AddSingleton<IAssignmentControllerService, AssignmentControllerService>();
builder.Services.AddSingleton<IGeofenceControllerService, GeofenceControllerService>();
builder.Services.AddSingleton<IIvListControllerService, IvListControllerService>();
builder.Services.AddSingleton<IWebhookControllerService, WebhookControllerService>();
builder.Services.AddSingleton<ITimeZoneService, TimeZoneService>();
// TODO: Remove extra service registration of 'JobControllerService' or confirm there are no issues and two instances are not created
builder.Services.AddSingleton<IJobControllerServiceHost, JobControllerService>();
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

builder.Services.AddSingleton<IPluginCacheAccessorBootstrapper, DefaultStaticPluginCacheAccessorBootstrapper>();
builder.Services.AddSingleton<ICachedPluginAssembly, CachedPluginAssembly>();
builder.Services.AddSingleton<IPluginCache, DefaultScopedPluginCache>();

// Register plugin host handlers
var uiHost = new UiHost(new Logger<IUiHost>(LoggerFactory.Create(x => x.AddConsole())));
var databaseHost = new DatabaseHost(new Logger<IDatabaseHost>(LoggerFactory.Create(x => x.AddConsole())), connectionString);
var loggingHost = new LoggingHost(new Logger<ILoggingHost>(LoggerFactory.Create(x => x.AddConsole())));
builder.Services.AddSingleton<ILoggingHost>(loggingHost);
builder.Services.AddSingleton<IDatabaseHost>(databaseHost);
builder.Services.AddSingleton<ILocalizationHost>(Translator.Instance);
builder.Services.AddSingleton<IUiHost>(uiHost);

// Load host applications default sidebar nav headers
await LoadUiAsync(uiHost);

// TODO: Use builder.Services registered instead of 'sharedServiceHosts'
var sharedServiceHosts = new Dictionary<Type, object>
{
    { typeof(ILoggingHost), loggingHost },
    // TODO: Break out JobControllerService to fix shared service host instance injection for plugins
    { typeof(IJobControllerServiceHost), new JobControllerService(
        new Logger<IJobControllerService>(LoggerFactory.Create(x => x.AddConsole())),
        null,
        null,
        null,
        null,
        null,
        null,
        null,
        null) },
    { typeof(IDatabaseHost), databaseHost },
    { typeof(ILocalizationHost), Translator.Instance },
    { typeof(IUiHost), uiHost },
};
// Instantiate 'IPluginManager' singleton with configurable options
var pluginManager = PluginManager.InstanceWithOptions(new PluginManagerOptions
{
    Configuration = builder.Configuration,
    RootPluginDirectory = Strings.PluginsFolder,
    SharedServiceHosts = sharedServiceHosts,
});
// Find plugins, register plugin services, load plugin assemblies, call OnLoad callback and register with 'IPluginManager'
await builder.Services.LoadPluginsAsync(pluginManager, builder.Environment.ContentRootPath);

// Configure custom 'Views' location paths to search in plugin sub directories
builder.Services.Configure<RazorViewEngineOptions>(options =>
{
    // Get a list of root plugin folder names in the "./bin/debug/plugins/*" base folder
    var pluginFolderNames = pluginManager.GetPluginFolderNames();
    // Register new View location searcher that includes absolute plugin related 'Views' folder paths
    var viewLocationExpander = new PluginViewLocationExpander(pluginManager.Options.RootPluginDirectory, pluginFolderNames ?? new List<string>());
    options.ViewLocationExpanders.Add(viewLocationExpander);
});

builder.Services.Configure<MvcRazorRuntimeCompilationOptions>(options =>
{
    //var libraryPath = Path.GetFullPath(
    //    Path.Combine(builder.Environment.ContentRootPath, "..", "MyClassLib"));
    //options.FileProviders.Add(new PhysicalFileProvider(libraryPath));
});

builder.Services.AddSingleton<IPluginManagerOptions>(pluginManager.Options);
builder.Services.AddSingleton<IPluginManager>(pluginManager);

#endregion

#region App Builder

var app = builder.Build();

//pluginManager.PluginHostAdded += OnPluginHostAdded;
//pluginManager.PluginHostRemoved += OnPluginHostRemoved;
//pluginManager.PluginHostStateChanged += OnPluginHostStateChanged;

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

// https://stackoverflow.com/a/64874175
app.UseCookiePolicy(new CookiePolicyOptions()
{
    MinimumSameSitePolicy = SameSiteMode.Lax
});

//app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// User authentication
app.UseAuthentication();
app.UseAuthorization();

// gRPC listener server services
app.MapGrpcService<ProtoPayloadServerService>();
app.MapGrpcService<TrainerInfoServerService>();
app.MapGrpcService<WebhookEndpointServerService>();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

// Call 'Configure' method in plugins
Configure(app, app.Services);

app.Run();

#endregion

#region Plugin Callback/Event Handlers

void Configure(WebApplication app, IServiceProvider serviceProvider)
{
    try
    {
        foreach (var (_, plugin) in pluginManager.Plugins)
        {
            // Call 'Configure(IApplicationBuilder)' event handler for each plugin
            plugin.Plugin.Configure(app);

            // TODO: Call Plugin.OnLoad() here instead of from ServiceCollectionExtensions
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while calling the 'Configure(IApplicationBuilder)' method in plugins.");
    }
}

async Task LoadUiAsync(IUiHost host)
{
    var navbarHeaders = new List<NavbarHeader>
    {
        new("Home", "Home", displayIndex: 0, icon: "fa-solid fa-fw fa-house"),
        new("Accounts", "Account", displayIndex: 1, icon: "fa-solid fa-fw fa-user"),
        new("Devices", displayIndex: 2, icon: "fa-solid fa-fw fa-mobile-alt", isDropdown: true, dropdownItems: new List<NavbarHeader>
        {
            new("Devices", "Device", "Index", displayIndex: 0, icon: "fa-solid fa-fw fa-layer-group"),
            new("Device Groups", "DeviceGroup", "Index", displayIndex: 1, icon: "fa-solid fa-fw fa-mobile-alt"),
        }),
        new("Instances", displayIndex: 3, icon: "fa-solid fa-fw fa-cubes-stacked", isDropdown: true, dropdownItems: new List<NavbarHeader>
        {
            new("Geofences", "Geofence", "Index", displayIndex: 0, icon: "fa-solid fa-fw fa-map-marked"),
            new("Instances", "Instance", "Index", displayIndex: 1, icon: "fa-solid fa-fw fa-cubes-stacked"),
            new("IV Lists", "IvList", "Index", displayIndex: 2, icon: "fa-solid fa-fw fa-list"),
        }),
        new("Plugins", "Plugin", displayIndex: 4, icon: "fa-solid fa-fw fa-puzzle-piece"),
        new("Schedules", displayIndex: 5, icon: "fa-solid fa-fw fa-calendar-days", isDropdown: true, dropdownItems: new List<NavbarHeader>
        {
            new("Assignments", "Assignment", "Index", displayIndex: 0, icon: "fa-solid fa-fw fa-cog"),
            new("Assignment Groups", "AssignmentGroup", "Index", displayIndex: 1, icon: "fa-solid fa-fw fa-cogs"),
        }),
        new("Webhooks", "Webhook", displayIndex: 6, icon: "fa-solid fa-fw fa-circle-nodes"),
        new("Users", "User", displayIndex: 7, icon: "fa-solid fa-fw fa-users"),
        new("Utilities", displayIndex: 8, icon: "fa-solid fa-fw fa-toolbox", isDropdown: true, dropdownItems: new List<NavbarHeader>
        {
            new("Clear Quests", "Utilities", "ClearQuests", displayIndex: 0, icon: "fa-solid fa-fw fa-broom"),
            new("Convert Forts", "Utilities", "ConvertForts", displayIndex: 1, icon: "fa-solid fa-fw fa-arrows-up-down"),
            new("Clear Stale Pokestops", "Utilities", "ClearStalePokestops", displayIndex: 2, icon: "fa-solid fa-fw fa-clock"),
            new("Reload Instance", "Utilities", "ReloadInstance", displayIndex: 3, icon: "fa-solid fa-fw fa-rotate"),
            new("Truncate Data", "Utilities", "TruncateData", displayIndex: 4, icon: "fa-solid fa-fw fa-trash-can"),
            new("Re-Quest", "Utilities", "ReQuest", displayIndex: 5, icon: "fa-solid fa-fw fa-clock-rotate-left"),
            new("Route Generator", "Utilities", "RouteGenerator", displayIndex: 6, icon: "fa-solid fa-fw fa-route"),
        }),
    };
    await uiHost.AddNavbarHeadersAsync(navbarHeaders);
}

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
        {
            // Get cached plugin state from database
            var dbPlugin = await context.Plugins.FindAsync(e.PluginHost.Plugin.Name);
            if (dbPlugin != null)
            {
                // Plugin host is cached in database, set previous plugin state,
                // otherwise set state from param
                //var isStateSet = state != dbPlugin.State && state != PluginState.Unset;
                //pluginHost.SetState(isStateSet ? state : dbPlugin.State);
                e.PluginHost.SetState(dbPlugin.State);
            }
            else
            {
                // Plugin host is not cached in database. Set current state to plugin
                // host and add insert into database
                e.PluginHost.SetState(e.PluginHost.State);
                dbPlugin = new Plugin
                {
                    Name = e.PluginHost.Plugin.Name,
                    State = e.PluginHost.State,
                };
            }

            // Save plugin host to database
            if (context.Plugins.Any(x => x.Name == e.PluginHost.Plugin.Name))
            {
                context.Plugins.Update(dbPlugin);
            }
            else
            {
                await context.Plugins.AddAsync(dbPlugin);
            }
            await context.SaveChangesAsync();
        }

    }
}

#endregion

#region Helpers

void RegisterAuthProviders(AuthenticationBuilder auth)
{
    var authConfig = config.GetSection("Authentication");

    // Check if GitHub auth is enabled, if so register it
    if (bool.TryParse(authConfig["GitHub:Enabled"], out var githubEnabled) && githubEnabled)
    {
        auth.AddGitHub(options =>
        {
            var github = authConfig.GetSection("GitHub");
            // Ensure GitHub auth is set
            if (github != null)
            {
                options.ClientId = github["ClientId"];
                options.ClientSecret = github["ClientSecret"];
                //options.Scope("");
            }
        });
    }

    // Check if Google auth is enabled, if so register it
    if (bool.TryParse(authConfig["Google:Enabled"], out var googleEnabled) && googleEnabled)
    {
        auth.AddGoogle(options =>
        {
            var google = authConfig.GetSection("Google");
            // Ensure Google auth is set
            if (google != null)
            {
                options.ClientId = google["ClientId"];
                options.ClientSecret = google["ClientSecret"];
                options.Scope.Add("email");
                options.Scope.Add("profile");
                options.Scope.Add("openid");
            }
        });
    }

    // Check if Discord auth is enabled, if so register it
    if (bool.TryParse(authConfig["Discord:Enabled"], out var discordEnabled) && discordEnabled)
    {
        auth.AddDiscord(options =>
        {
            var discord = authConfig.GetSection("Discord");
            // Ensure Discord auth is set
            if (discord != null)
            {
                options.ClientId = discord["ClientId"];
                options.ClientSecret = discord["ClientSecret"];
                options.Scope.Add("email");
                options.Scope.Add("guilds");
                options.SaveTokens = true;
            }
        });
    }
}

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
            //RequireConfirmedAccount = true,
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