namespace ChuckProtoParser
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.OpenApi.Models;
    using StackExchange.Redis;

    using Chuck.Data.Contexts;
    using Chuck.Data.Interfaces;
    using Chuck.Data.Repositories;

    public class Startup
    {
        public static string DbConnectionString { get; set; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "ChuckProtoParser", Version = "v1" });
            });

            services.AddDbContextFactory<DeviceControllerContext>(options =>
                options.UseMySql(DbConnectionString, ServerVersion.AutoDetect(DbConnectionString)), ServiceLifetime.Singleton);
            services.AddDbContext<DeviceControllerContext>(options =>
                //options.UseMySQL(DbConfig.ToString()));
                options.UseMySql(DbConnectionString, ServerVersion.AutoDetect(DbConnectionString)), ServiceLifetime.Scoped);
            services.AddScoped(typeof(IAsyncRepository<>), typeof(EfCoreRepository<,>));

            var options = new ConfigurationOptions
            {
                EndPoints =
                {
                    { $"{Configuration["Redis:Host"]}:{Configuration["Redis:Port"]}" }
                },
                Password = Configuration["Redis:Password"],
            };
            services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(options));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "ChuckProtoParser v1"));
            }

            //app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}