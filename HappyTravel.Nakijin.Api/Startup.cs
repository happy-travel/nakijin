using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using FloxDc.CacheFlow.Extensions;
using HappyTravel.ErrorHandling.Extensions;
using HappyTravel.Nakijin.Api.Infrastructure;
using HappyTravel.Nakijin.Api.Infrastructure.Environments;
using HappyTravel.Nakijin.Api.Services.LocationMappingInfo;
using HappyTravel.StdOutLogger.Extensions;
using HappyTravel.Telemetry.Extensions;
using HappyTravel.VaultClient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace HappyTravel.Nakijin.Api
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

            services.ConfigureServiceOptions(Configuration, vaultClient)
                .AddServices();

            var redisEndpoint = EnvironmentVariableHelper.Get("Redis:Endpoint", Configuration);
            services.AddHealthChecks()
                .AddCheck<ControllerResolveHealthCheck>(nameof(ControllerResolveHealthCheck))
                .AddRedis(EnvironmentVariableHelper.Get("Redis:Endpoint", Configuration));

            services.AddResponseCompression()
                .AddHttpContextAccessor()
                .AddCors()
                .AddLocalization()
                .AddMemoryCache()
                .AddTracing(Configuration, options =>
                {
                    options.ServiceName = $"{_environment.ApplicationName}-{_environment.EnvironmentName}";
                    options.JaegerHost = _environment.IsLocal()
                        ? Configuration.GetValue<string>("Jaeger:AgentHost")
                        : Configuration.GetValue<string>(Configuration.GetValue<string>("Jaeger:AgentHost"));
                    options.JaegerPort = _environment.IsLocal()
                        ? Configuration.GetValue<int>("Jaeger:AgentPort")
                        : Configuration.GetValue<int>(Configuration.GetValue<string>("Jaeger:AgentPort"));
                    options.RedisEndpoint = Configuration.GetValue<string>(Configuration.GetValue<string>("Redis:Endpoint"));
                });

            services.AddApiVersioning(options =>
            {
                options.AssumeDefaultVersionWhenUnspecified = false;
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.ReportApiVersions = true;
            });

            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1.0",
                    new OpenApiInfo { Title = "HappyTravel.com Property Management System API", Version = "v1.0" });

                var apiXmlCommentsFilePath = Path.Combine(AppContext.BaseDirectory,
                    $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
                options.IncludeXmlComments(apiXmlCommentsFilePath);

                foreach (var assembly in Assembly.GetExecutingAssembly().GetReferencedAssemblies())
                {
                    var path = Path.Combine(AppContext.BaseDirectory, $"{assembly.Name}.xml");
                    if (File.Exists(path))
                        options.IncludeXmlComments(path);
                }

                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey
                });
                options.AddSecurityRequirement(new OpenApiSecurityRequirement()
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            },
                            Scheme = "oauth2",
                            Name = "Bearer",
                            In = ParameterLocation.Header,
                        },
                        Array.Empty<string>()
                    }
                });
            });
            //  services.AddSwaggerGenNewtonsoftSupport();

            services.AddMvcCore()
                .AddFormatterMappings()
                .AddNewtonsoftJson()
                .AddApiExplorer()
                .AddCacheTagHelper()
                .AddControllersAsServices()
                .AddAuthorization(options =>
                {
                    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
                    options.AddPolicy("CanEdit", policy =>
                    {
                        policy.RequireClaim("scope", "mapper.edit");
                    });
                });

            services.AddTransient<ILocationMappingInfoService, LocationMappingInfoService>();
            services.AddTransient<ILocationMappingFactory, LocationMappingFactory>();
            services.AddStaticDataPublicationService(vaultClient, Configuration, _environment);
            services.AddProblemDetailsErrorHandling();
        }


        public void Configure(IApplicationBuilder app, IOptions<RequestLocalizationOptions> localizationOptions)
        {
            var logger = _loggerFactory.CreateLogger<Startup>();
            app.UseProblemDetailsErrorHandler(_environment, logger);
            app.UseHttpContextLogging(
                options => options.IgnoredPaths = new HashSet<string> { "/health" }
            );

            app.UseRequestLocalization(localizationOptions.Value);
            app.UseSwagger()
                .UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint("/swagger/v1.0/swagger.json", "HappyTravel.com StaticDataMapper API");
                    options.RoutePrefix = string.Empty;
                });

            app.UseResponseCompression()
                .UseCors(builder => builder
                    .AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod())
                .UseHttpsRedirection()
                .UseRouting()
                .UseAuthentication()
                .UseAuthorization()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapHealthChecks("/health").WithMetadata(new AllowAnonymousAttribute());
                    endpoints.MapControllers();
                });
        }


        public IConfiguration Configuration { get; }


        private readonly ILoggerFactory _loggerFactory;
        private readonly IWebHostEnvironment _environment;
    }
}