using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using FloxDc.CacheFlow.Extensions;
using HappyTravel.PropertyManagement.Api.Infrastructure;
using HappyTravel.ErrorHandling.Extensions;
using HappyTravel.PropertyManagement.Api.Infrastructure.Environments;
using HappyTravel.StdOutLogger.Extensions;
using HappyTravel.VaultClient;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace HappyTravel.PropertyManagement.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration, ILoggerFactory loggerFactory, IWebHostEnvironment environment)
        {
            Configuration = configuration;
            _environment = environment;
            _loggerFactory = loggerFactory;
        }


        public void ConfigureServices(IServiceCollection services)
        {
            var serializationSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Formatting = Formatting.None
            };
            JsonConvert.DefaultSettings = () => serializationSettings;

            using var vaultClient = new VaultClient.VaultClient(new VaultOptions
            {
                BaseUrl = new Uri(EnvironmentVariableHelper.Get("Vault:Endpoint", Configuration)),
                Engine = Configuration["Vault:Engine"],
                Role = Configuration["Vault:Role"]
            });
            vaultClient.Login(EnvironmentVariableHelper.Get("Vault:Token", Configuration)).GetAwaiter().GetResult();

            services.AddMemoryCache()
                .AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = EnvironmentVariableHelper.Get("Redis:Endpoint", Configuration);
                })
                .AddDoubleFlow();

            services.AddHttpClient();
            services.ConfigureServiceOptions(Configuration, vaultClient)
                .AddServices();


            services.AddHealthChecks()
                .AddCheck<ControllerResolveHealthCheck>(nameof(ControllerResolveHealthCheck))
                .AddRedis(EnvironmentVariableHelper.Get("Redis:Endpoint", Configuration));

            services.AddResponseCompression()
                .AddHttpContextAccessor()
                .AddCors()
                .AddLocalization()
                .AddMemoryCache()
                .AddTracing(_environment, Configuration);

            services.AddApiVersioning(options =>
            {
                options.AssumeDefaultVersionWhenUnspecified = false;
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.ReportApiVersions = true;
            });

            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1.0",
                    new OpenApiInfo {Title = "HappyTravel.com Property Management System API", Version = "v1.0"});

                var apiXmlCommentsFilePath = Path.Combine(AppContext.BaseDirectory,
                    $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
                options.IncludeXmlComments(apiXmlCommentsFilePath);

                foreach (var assembly in Assembly.GetExecutingAssembly().GetReferencedAssemblies())
                {
                    var path = Path.Combine(AppContext.BaseDirectory, $"{assembly.Name}.xml");
                    if (File.Exists(path))
                        options.IncludeXmlComments(path);
                }
            });

            services.AddMvcCore()
                .AddFormatterMappings()
                .AddNewtonsoftJson()
                .AddApiExplorer()
                .AddCacheTagHelper()
                .AddControllersAsServices();
        }


        public void Configure(IApplicationBuilder app)
        {
            var logger = _loggerFactory.CreateLogger<Startup>();
            app.UseProblemDetailsExceptionHandler(_environment, logger);
            app.UseHttpContextLogging(
                options => options.IgnoredPaths = new HashSet<string> {"/health"}
            );

            app.UseSwagger()
                .UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint("/swagger/v1.0/swagger.json", "HappyTravel.com ServiceName API");
                    options.RoutePrefix = string.Empty;
                });

            app.UseResponseCompression()
                .UseCors(builder => builder
                    .AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod())
                .UseHttpsRedirection()
                .UseRouting()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapHealthChecks("/health");
                    endpoints.MapControllers();
                });
        }


        public IConfiguration Configuration { get; }


        private readonly ILoggerFactory _loggerFactory;
        private readonly IWebHostEnvironment _environment;
    }
}