using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using HappyTravel.StaticDataMapper.Data.Models;
using HappyTravel.StaticDataMapper.Data;
using HappyTravel.SecurityClient;
using HappyTravel.StaticDataMapper.Api.Infrastructure.Environments;
using HappyTravel.StaticDataMapper.Api.Services;
using HappyTravel.StaticDataMapper.Api.Services.Workers;
using IdentityModel;
using LocationNameNormalizer.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Trace.Samplers;
using Polly;
using Polly.Extensions.Http;

namespace HappyTravel.StaticDataMapper.Api.Infrastructure
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddServices(this IServiceCollection services)
        {
            services.AddSingleton<IAccommodationsTreesCache, AccommodationsTreesCache>();
            services.AddSingleton<ILocalitiesCache, LocalitiesCache>();
            services.AddSingleton<ICountriesCache, CountriesCache>();

            services.AddTransient<IAccommodationPreloader, AccommodationPreloader>();
            services.AddTransient<IAccommodationMapper, AccommodationMapper>();
            services.AddTransient<IAccommodationsDataMerger, AccommodationDataMerger>();
            services.AddTransient<ILocationMapper, LocationMapper>();
            services.AddTransient<IAccommodationService, AccommodationService>();
            services.AddTransient<ISuppliersPriorityService, SuppliersPriorityService>();
            services.AddTransient<IConnectorClient, ConnectorClient>();
            services.AddSingleton<ISecurityTokenManager, SecurityTokenManager>();

            services.AddNameNormalizationServices();

            return services;
        }


        public static IServiceCollection AddTracing(this IServiceCollection services, IWebHostEnvironment environment,
            IConfiguration configuration)
        {
            string agentHost;
            int agentPort;
            if (environment.IsLocal())
            {
                agentHost = configuration["Jaeger:AgentHost"];
                agentPort = int.Parse(configuration["Jaeger:AgentPort"]);
            }
            else
            {
                agentHost = EnvironmentVariableHelper.Get("Jaeger:AgentHost", configuration);
                agentPort = int.Parse(EnvironmentVariableHelper.Get("Jaeger:AgentPort", configuration));
            }

            var serviceName = $"{nameof(StaticDataMapper)}-{environment.EnvironmentName}";
            services.AddOpenTelemetry(builder =>
            {
                builder.UseJaegerExporter(options =>
                    {
                        options.ServiceName = serviceName;
                        options.AgentHost = agentHost;
                        options.AgentPort = agentPort;
                    })
                    .AddRequestInstrumentation()
                    .AddDependencyInstrumentation()
                    .SetResource(Resources.CreateServiceResource(serviceName))
                    .SetSampler(new AlwaysOnSampler());
            });

            return services;
        }


        public static IServiceCollection ConfigureServiceOptions(this IServiceCollection services,
            IConfiguration configuration, VaultClient.VaultClient vaultClient)
        {
            var databaseOptions = vaultClient.Get(configuration["Nakijin:Database:Options"]).GetAwaiter().GetResult();
            services.AddEntityFrameworkNpgsql().AddDbContextPool<NakijinContext>(options =>
            {
                var host = databaseOptions["host"];
                var port = databaseOptions["port"];
                var password = databaseOptions["password"];
                var userId = databaseOptions["userId"];

                var connectionString = configuration.GetConnectionString("Nakijin");
                options.UseNpgsql(string.Format(connectionString, host, port, userId, password), builder =>
                {
                    builder.UseNetTopologySuite();
                    builder.EnableRetryOnFailure();
                });
                options.EnableSensitiveDataLogging(false);
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            }, 16);

            services.Configure<AccommodationsPreloaderOptions>(o =>
            {
                var suppliers = EnvironmentVariableHelper.Get("Nakijin:Preloader:Suppliers", configuration);
                var batchSize = EnvironmentVariableHelper.Get("Nakijin:Preloader:BatchSize", configuration);
                o.Suppliers = string.IsNullOrEmpty(suppliers)
                    ? Enum.GetValues(typeof(Suppliers)).Cast<Suppliers>().ToList()
                    : suppliers.Split(";").Cast<Suppliers>().ToList();
                o.BatchSize = string.IsNullOrEmpty(batchSize) ? 1000 : int.Parse(batchSize);
            });

            var suppliersOptions = vaultClient.Get(configuration["Nakijin:Suppliers:Options"]).GetAwaiter().GetResult();
            services.Configure<SuppliersOptions>(options =>
            {
                options.SuppliersUrls = !suppliersOptions.Any()
                    ? new Dictionary<Suppliers, string>()
                    : suppliersOptions.ToDictionary(s
                        => Enum.Parse<Suppliers>(s.Key, true), s => s.Value);
            });

            var clientOptions = vaultClient.Get(configuration["Nakijin:Client:Options"]).GetAwaiter().GetResult();
            var authorityOptions = vaultClient.Get(configuration["Nakijin:Authority:Options"]).GetAwaiter().GetResult();
            services.Configure<TokenRequestOptions>(options =>
            {
                var authorityUrl = authorityOptions["authorityUrl"];
                var uri = new Uri(new Uri(authorityUrl), "/connect/token");
                options.Address = uri.ToString();
                options.ClientId = clientOptions["clientId"];
                options.ClientSecret = clientOptions["clientSecret"];
                options.Scope = clientOptions["scope"];
                options.GrantType = OidcConstants.GrantTypes.ClientCredentials;
            });

            return services;
        }


        private static IAsyncPolicy<HttpResponseMessage> GetDefaultRetryPolicy()
        {
            var jitter = new Random();

            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(3, attempt
                    => TimeSpan.FromMilliseconds(Math.Pow(500, attempt)) +
                    TimeSpan.FromMilliseconds(jitter.Next(0, 100)));
        }
    }
}