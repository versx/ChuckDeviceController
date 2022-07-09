namespace ChuckDeviceController
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;

    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Diagnostics.HealthChecks;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.HttpsPolicy;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.FileProviders;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.OpenApi.Models;
    using StackExchange.Redis;

    using Chuck.Common;
    using Chuck.Configuration;
    using Chuck.Data.Contexts;
    using Chuck.Data.Entities;
    using Chuck.Data.Interfaces;
    using Chuck.Data.Repositories;
    using Chuck.Extensions;
    using Chuck.Net.Middleware;
    using ChuckDeviceController.JobControllers;

    public class Startup
    {
        public static Config Config { get; set; }

        public static DatabaseConfig DbConfig => Config?.Database;

        private IConnectionMultiplexer _redis;
        private ISubscriber _subscriber;
        private IInstanceController _instanceController;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            /*
            services.AddSingleton<IConfiguration>(provider => new ConfigurationBuilder()
                    .AddEnvironmentVariables()
                    .AddJsonFile("config.json", optional: false, reloadOnChange: true)
                    .Build());
            */

            services.AddHealthChecks()
                .AddMySql(DbConfig.ToString())
                .AddRedis($"{Config.Redis.Host}:{Config.Redis.Port}")
                .AddProcessHealthCheck(Process.GetCurrentProcess().ProcessName, p => p.Length >= 1)
                .AddProcessAllocatedMemoryHealthCheck((int)Environment.WorkingSet)
                .AddDiskStorageHealthCheck(setup =>
                {
                    foreach (var drive in DriveInfo.GetDrives())
                    {
                        if (drive.IsReady && drive.DriveType == DriveType.Fixed)
                        {
                            setup.AddDrive(drive.RootDirectory.FullName);
                        }
                    }
                })
                //.AddDnsResolveHealthCheck(setup => setup.ResolveHost("https://google.com"))
                .AddPingHealthCheck(setup => setup.AddHost("discord.com", 10), "discord");

                services.AddHealthChecksUI(settings =>
                {
                    settings.AddHealthCheckEndpoint("Main Health Check", "/health");
                    settings.MaximumHistoryEntriesPerEndpoint(50);
                })
                    .AddInMemoryStorage();

            // Save sessions to the database
            // TODO: Allow for custom column names
            services.AddDistributedMemoryCache();
            services.AddDistributedMySqlCache(options =>
            {
                options.ConnectionString = DbConfig.ToString();
                //options.DefaultSlidingExpiration
                options.ExpiredItemsDeletionInterval = TimeSpan.FromHours(1);
                options.SchemaName = DbConfig.Database;
                options.TableName = "session";
            });
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(60);
                options.Cookie.Name = "cdc.session";
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            // Register IAssignmentController and IInstanceController's with DI
            services.AddSingleton(typeof(IInstanceController), typeof(InstanceController));
            services.AddSingleton(typeof(IAssignmentController), typeof(AssignmentController));

            services.AddSwaggerGen(c => c.SwaggerDoc("v1", new OpenApiInfo { Title = "ChuckDeviceController", Version = "v1" }));

            services.AddDbContextFactory<DeviceControllerContext>(options =>
                options.UseMySql(DbConfig.ToString(), ServerVersion.AutoDetect(DbConfig.ToString())), ServiceLifetime.Singleton);
            services.AddDbContext<DeviceControllerContext>(options =>
                //options.UseMySQL(DbConfig.ToString()));
                options.UseMySql(DbConfig.ToString(), ServerVersion.AutoDetect(DbConfig.ToString())), ServiceLifetime.Scoped);
            /*
            services.AddDbContextPool<DeviceControllerContext>(
                options => options.UseMySql(ServerVersion.AutoDetect(DbConfig.ToString()),
                    mySqlOptions =>
                    {
                        mySqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 3,
                            maxRetryDelay: TimeSpan.FromSeconds(5),
                            errorNumbersToAdd: null
                        );
                    }
            ), 128); // TODO: Configurable PoolSize (128=default)
            */

            services.AddScoped(typeof(IAsyncRepository<>), typeof(EfCoreRepository<,>));
            services.AddScoped<Config>();

            // Redis
            var options = new ConfigurationOptions
            {
                AbortOnConnectFail = false,
                EndPoints =
                {
                    { $"{Config.Redis.Host}:{Config.Redis.Port}" }
                },
                Password = Config.Redis.Password,
            };
            
            try
            {
                services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(options));
            }
            catch (Exception ex)
            {
                ConsoleExt.WriteError(ex.Message);
                Environment.Exit(0);
            }

            // Cross origin resource sharing configuration
            services.AddCors(option => option.AddPolicy("Test", builder => {
                builder.AllowAnyOrigin()
                       .AllowAnyHeader()
                       .AllowAnyMethod();
            }));

            // Profiling
            // The services.AddMemoryCache(); code is required - there is a bug in
            // MiniProfiler, if we have not configured MemoryCache, it will fail.
            if (Config.EnableProfiler)
            {
                services.AddMemoryCache();
                services.AddEntityFrameworkMySql().AddDbContext<DeviceControllerContext>();
                services.AddMiniProfiler(options =>
                {
                    options.RouteBasePath = "/profiler";
                    options.EnableMvcViewProfiling = true;
                    options.EnableMvcFilterProfiling = true;
                    options.EnableServerTimingHeader = true;
                    options.ShowControls = true;
                    options.TrackConnectionOpenClose = true;
                }).AddEntityFramework();
            }

            services.AddResponseCaching();

            services.AddControllers();
            services.AddControllersWithViews();

            // TODO: Better impl, use singleton class
            _redis = ConnectionMultiplexer.Connect(options);
            _subscriber = _redis.GetSubscriber();
            _subscriber.Subscribe("*", PokemonSubscriptionHandler);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseSession();

            // Discord auth middleware
            if (Config.Discord?.Enabled ?? false)
            {
                app.UseMiddleware<DiscordAuthMiddleware>();
            }
            if (Config.DeviceAuth?.AllowedHosts?.Count > 0)
            {
                app.UseMiddleware<ValidateHostMiddleware>(Config.DeviceAuth?.AllowedTokens);
            }
            if (Config.DeviceAuth?.AllowedTokens?.Count > 0)
            {
                app.UseMiddleware<TokenAuthMiddleware>(Config.DeviceAuth?.AllowedTokens);
            }

            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", $"{Strings.AppName} v1"));

            if (Config.EnableProfiler)
            {
                app.UseMiniProfiler();
            }

            //app.UseHttpsRedirection();
            app.UseDefaultFiles();
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(
                    Path.Combine(env.WebRootPath, "static")),
                RequestPath = "/static"
            });

            app.UseCors("Test");

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/health", new HealthCheckOptions
                {
                    ResponseWriter = HealthChecks.UI.Client.UIResponseWriter.WriteHealthCheckUIResponse,
                });
                endpoints.MapHealthChecksUI(opt =>
                {
                    opt.UIPath = "/health-ui";
                    opt.ResourcesPath = "/health";
                });
            });

            _instanceController = app.ApplicationServices.GetRequiredService<IInstanceController>();
        }

        // TODO: Create DI class to handle 
        private void PokemonSubscriptionHandler(RedisChannel channel, RedisValue message)
        {
            if (_instanceController == null)
            {
                Console.WriteLine($"[Startup] InstanceController not initialized yet");
                return;
            }
            switch (channel)
            {
                case RedisChannels.PokemonAdded:
                    {
                        var pokemon = message.ToString().FromJson<Pokemon>();
                        if (pokemon != null)
                        {
                            ThreadPool.QueueUserWorkItem(x => _instanceController.GotPokemon(pokemon));
                        }
                        break;
                    }
                case RedisChannels.PokemonUpdated:
                    {
                        var pokemon = message.ToString().FromJson<Pokemon>();
                        if (pokemon != null)
                        {
                            ThreadPool.QueueUserWorkItem(x => _instanceController.GotIV(pokemon));
                        }
                        break;
                    }
            }
        }
    }
}
