using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using ChuckDeviceConfigurator;
using ChuckDeviceConfigurator.Data;
using ChuckDeviceConfigurator.Services.Jobs;
using ChuckDeviceController.Data.Contexts;


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

builder.Services.Configure<IdentityOptions>(options =>
{
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequiredUniqueChars = 1;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    //options.SignIn.RequireConfirmedAccount = true;
    options.SignIn.RequireConfirmedEmail = true;
    //options.User.RequireUniqueEmail = true;
    //options.Stores.ProtectPersonalData = true;
    //options.ClaimsIdentity.EmailClaimType
});

builder.Services.ConfigureApplicationCookie(options =>
{
    // Cookie settings
    options.Cookie.HttpOnly = true;
    //options.Cookie.Expiration 

    options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.SlidingExpiration = true;
    //options.ReturnUrlParameter=""
});

// Register external 3rd party authentication providers if configured
var auth = builder.Services.AddAuthentication();
RegisterAuthProviders(auth);

#endregion

// Add services to the container.
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

builder.Services.AddDbContextFactory<DeviceControllerContext>(options =>
{
    options.EnableSensitiveDataLogging()
           .UseMySql(connectionString, serverVersion, opt =>
           {
               opt.MigrationsAssembly(Strings.AssemblyName);
           });
}, ServiceLifetime.Singleton);

builder.Services.AddDbContext<DeviceControllerContext>(options =>
{
    options.EnableSensitiveDataLogging()
           .UseMySql(connectionString, serverVersion, opt =>
           {
               opt.MigrationsAssembly(Strings.AssemblyName);
           });
}, ServiceLifetime.Scoped);

builder.Services.AddSingleton<IJobControllerService, JobControllerService>();



var app = builder.Build();

// Seed default user and roles
await SeedDefaultData(app.Services);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

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

async Task SeedDefaultData(IServiceProvider serviceProvider)
{
    using (var scope = serviceProvider.CreateScope())
    {
        var services = scope.ServiceProvider;
        var loggerFactory = services.GetRequiredService<ILoggerFactory>();
        try
        {
            var context = services.GetRequiredService<UserIdentityContext>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

            // Seed default user roles
            await UserIdentityContextSeed.SeedRolesAsync(userManager, roleManager);

            // Seed default SuperAdmin user
            await UserIdentityContextSeed.SeedSuperAdminAsync(userManager, roleManager);
        }
        catch (Exception ex)
        {
            var logger = loggerFactory.CreateLogger<Program>();
            logger.LogError(ex, "An error occurred while seeding the database.");
        }
    }
}