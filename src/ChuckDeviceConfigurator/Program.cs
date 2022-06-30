using ChuckDeviceConfigurator;
using ChuckDeviceConfigurator.Areas.Identity.Data;
using ChuckDeviceConfigurator.Data;
using ChuckDeviceController.Data.Contexts;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;


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

/*
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
*/

#endregion

// Add services to the container.
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

builder.Services.AddDbContext<DeviceControllerContext>(options =>
{
    options.EnableSensitiveDataLogging()
           .UseMySql(connectionString, serverVersion, opt =>
           {
               opt.MigrationsAssembly(Strings.AssemblyName);
           });
}, ServiceLifetime.Scoped);


var app = builder.Build();

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