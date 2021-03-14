namespace ChuckDeviceController
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Net.Mime;
    using System.Threading;

    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Diagnostics.HealthChecks;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.HttpsPolicy;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Diagnostics.HealthChecks;
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
    using ChuckDeviceController.JobControllers;

    public class Startup
    {
        public static Config Config { get; set; }

        public static DatabaseConfig DbConfig => Config?.Database;

        private IConnectionMultiplexer _redis;
        private ISubscriber _subscriber;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
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

            //services.AddRazorPages();
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

            services.AddHealthChecks();
            services.AddScoped(typeof(IAsyncRepository<>), typeof(EfCoreRepository<,>));
            services.AddScoped<Config>();

            var options = new ConfigurationOptions
            {
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

            services.AddCors(option => option.AddPolicy("Test", builder =>
            {
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

            //services.AddDistributedMemoryCache();
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
            app.UseHealthChecks("/health",
                new HealthCheckOptions
                {
                    ResponseWriter = async (context, report) =>
                    {
                        var result = new
                        {
                            status = report.Status.ToString(),
                            errors = report.Entries.Select(e => new
                            {
                                key = e.Key,
                                value = Enum.GetName(typeof(HealthStatus), e.Value.Status)
                            })
                        }.ToJson();
                        context.Response.ContentType = MediaTypeNames.Application.Json;
                        await context.Response.WriteAsync(result).ConfigureAwait(false);
                    }
                });

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
                endpoints.MapHealthChecks("home_page_health_check");
                endpoints.MapHealthChecks("api_health_check");
                //endpoints.MapRazorPages();
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
                            ThreadPool.QueueUserWorkItem(_ => InstanceController.Instance.GotPokemon(pokemon));
                        }
                        break;
                    }
                case RedisChannels.PokemonUpdated:
                    {
                        var pokemon = message.ToString().FromJson<Pokemon>();
                        if (pokemon != null)
                        {
                            ThreadPool.QueueUserWorkItem(_ => InstanceController.Instance.GotIV(pokemon));
                        }
                        break;
                    }
            }
        }
    }
}