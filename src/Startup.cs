namespace ChuckDeviceController
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Net.Mime;

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

    using ChuckDeviceController.Configuration;
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Interfaces;
    using ChuckDeviceController.Data.Repositories;
    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.JobControllers;
    using ChuckDeviceController.Services;

    public class Startup
    {
        public static Config Config { get; private set; }

        public static DatabaseConfig DbConfig { get; private set; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            var configPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                Path.Combine("..", Strings.DefaultConfigFileName)
            );
            Config = Config.Load(configPath);
            if (Config == null)
            {
                Console.WriteLine($"Failed to load config {configPath}");
                return;
            }
            DbConfig = Config.Database;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public async void ConfigureServices(IServiceCollection services)
        {
            await AssignmentController.Instance.Initialize();

            //services.AddRazorPages();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "ChuckDeviceController", Version = "v1" });
            });
            services.AddDbContextFactory<DeviceControllerContext>(options =>
                options.UseMySql(DbConfig.ToString(), ServerVersion.AutoDetect(DbConfig.ToString())), ServiceLifetime.Singleton);
            services.AddDbContext<DeviceControllerContext>(options =>
                //options.UseMySQL(DbConfig.ToString()));
                options.UseMySql(DbConfig.ToString(), ServerVersion.AutoDetect(DbConfig.ToString())), ServiceLifetime.Scoped);

            services.AddHealthChecks();
            services.AddScoped(typeof(IRepository<>), typeof(EfCoreRepository<,>));
            //services.AddScoped<IConsumerService, ConsumerService>();
            services.AddSingleton<IConsumerService, ConsumerService>();
            services.AddScoped<Config>();
            //services.addhos

            services.AddCors(option => option.AddPolicy("Test", builder => {
                builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();

            }));

            services.AddResponseCaching();
            //services.AddMemoryCache();
            //services.AddDistributedMemoryCache();
            services.AddControllers();
            services.AddControllersWithViews();
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
                        await context.Response.WriteAsync(result);
                    }
                });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "ChuckDeviceController v1"));
            }

            //app.UseRequestResponseLogging();

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
    }
}