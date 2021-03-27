namespace ChuckDeviceController
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
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
    using Chuck.Data.Contexts;
    using Chuck.Data.Entities;
    using Chuck.Data.Interfaces;
    using Chuck.Data.Repositories;
    using Chuck.Extensions;
    using Chuck.Net.Middleware;
    using ChuckDeviceController.JobControllers;

    public class Startup
    {
        public static string DbConnectionString { get; set; }

        private IConnectionMultiplexer _redis;
        private ISubscriber _subscriber;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            ConsoleExt.WriteDebug($"Available Environment Variables");
            foreach (var c in Configuration.AsEnumerable())
            {
                ConsoleExt.WriteDebug(c.Key + "=" + c.Value);
            }
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public async void ConfigureServices(IServiceCollection services)
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
                options.ConnectionString = DbConnectionString;
                //options.DefaultSlidingExpiration
                options.ExpiredItemsDeletionInterval = TimeSpan.FromHours(1);
                options.SchemaName = DbConnectionString.GetBetween("Database=", ";");//DbConfig.Database;
                options.TableName = "session";
            });
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(60);
                options.Cookie.Name = "cdc.session";
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            services.AddSwaggerGen(c => c.SwaggerDoc("v1", new OpenApiInfo { Title = "ChuckDeviceController", Version = "v1" }));

            services.AddDbContextFactory<DeviceControllerContext>(options =>
                options.UseMySql(DbConnectionString, ServerVersion.AutoDetect(DbConnectionString)), ServiceLifetime.Singleton);
            services.AddDbContext<DeviceControllerContext>(options =>
                //options.UseMySQL(DbConfig.ToString()));
                options.UseMySql(DbConnectionString, ServerVersion.AutoDetect(DbConnectionString)), ServiceLifetime.Scoped);
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

            // Redis
            var options = new ConfigurationOptions
            {
                EndPoints =
                {
                    { $"{Configuration["Redis:Host"]}:{Configuration["Redis:Port"]}" }
                },
                Password = Configuration["Redis:Password"],
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
            services.AddCors(option => option.AddPolicy("Test", builder =>
                builder.AllowAnyOrigin()
                       .AllowAnyHeader()
                       .AllowAnyMethod()
            ));

            // Profiling
            // The services.AddMemoryCache(); code is required - there is a bug in
            // MiniProfiler, if we have not configured MemoryCache, it will fail.
            if (bool.Parse(Configuration["EnableProfiler"]))
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

            await InstanceController.Instance.Start().ConfigureAwait(false);
            await AssignmentController.Instance.Start().ConfigureAwait(false);

            // TODO: Better impl, use singleton class
            _redis = ConnectionMultiplexer.Connect(options);
            _subscriber = _redis.GetSubscriber();
            _subscriber.Subscribe("*", PokemonSubscriptionHandler);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
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

            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", $"{Strings.AppName} v1"));

            if (Configuration.GetValue<bool>("EnableProfiler"))
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
        }

        private void PokemonSubscriptionHandler(RedisChannel channel, RedisValue message)
        {
            switch (channel)
            {
                case RedisChannels.PokemonAdded:
                    {
                        var pokemon = message.ToString().FromJson<Pokemon>();
                        if (pokemon != null)
                        {
                            ThreadPool.QueueUserWorkItem(x => InstanceController.Instance.GotPokemon(pokemon));
                        }
                        break;
                    }
                case RedisChannels.PokemonUpdated:
                    {
                        var pokemon = message.ToString().FromJson<Pokemon>();
                        if (pokemon != null)
                        {
                            ThreadPool.QueueUserWorkItem(x => InstanceController.Instance.GotIV(pokemon));
                        }
                        break;
                    }
            }
        }
    }

    internal static class StringExtensions
    {
        /// <summary>
        ///     A string extension method that get the string between the two specified string.
        /// </summary>
        /// <param name="this">The @this to act on.</param>
        /// <param name="before">The string before to search.</param>
        /// <param name="after">The string after to search.</param>
        /// <returns>The string between the two specified string.</returns>
        public static string GetBetween(this string value, string before, string after)
        {
            var beforeStartIndex = value.IndexOf(before);
            var startIndex = beforeStartIndex + before.Length;
            var afterStartIndex = value.IndexOf(after, startIndex);
            if (beforeStartIndex == -1 || afterStartIndex == -1)
            {
                return string.Empty;
            }
            //return value.Substring(startIndex, afterStartIndex - startIndex);
            return value[startIndex..afterStartIndex];
        }
    }
}
