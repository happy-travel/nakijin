using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using HappyTravel.StaticDataMapper.Data.Models;
using HappyTravel.StaticDataMapper.Data;
using HappyTravel.SecurityClient;
using HappyTravel.StaticDataMapper.Api.Infrastructure.Environments;
using HappyTravel.StaticDataMapper.Api.Services;
using HappyTravel.StaticDataMapper.Api.Services.Workers;
using IdentityModel;
using IdentityServer4.AccessTokenValidation;
using LocationNameNormalizer.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Localization.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Polly;
using Polly.Extensions.Http;
using StackExchange.Redis;

namespace HappyTravel.StaticDataMapper.Api.Infrastructure
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddServices(this IServiceCollection services)
        {
            services.AddSingleton<IAccommodationsTreesCache, AccommodationsTreesCache>();
            services.AddSingleton<ILocalitiesCache, LocalitiesCache>();
            services.AddSingleton<ICountriesCache, CountriesCache>();
            services.AddSingleton<ILocalityZonesCache, LocalityZonesCache>();

            services.AddTransient<IAccommodationPreloader, AccommodationPreloader>();
            services.AddTransient<IAccommodationMapper, AccommodationMapper>();
            services.AddTransient<IAccommodationsDataMerger, AccommodationDataMerger>();
            services.AddTransient<ILocationMapper, LocationMapper>();
            services.AddTransient<IAccommodationManagementService, AccommodationManagementService>();
            services.AddTransient<ISuppliersPriorityService, SuppliersPriorityService>();
            services.AddTransient<IConnectorClient, ConnectorClient>();
            services.AddSingleton<ISecurityTokenManager, SecurityTokenManager>();

            services.AddTransient<IAccommodationService, AccommodationService>();
            services.AddTransient<ILocationService, LocationService>();

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

            var connection = ConnectionMultiplexer.Connect(EnvironmentVariableHelper.Get("Redis:Endpoint", configuration));
            var serviceName = $"{nameof(StaticDataMapper)}-{environment.EnvironmentName}";
            
            services.AddOpenTelemetryTracing(builder =>
            {
                builder
                    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName))
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRedisInstrumentation(connection)
                    .AddJaegerExporter(options =>
                    {
                        options.AgentHost = agentHost;
                        options.AgentPort = agentPort;
                    })
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

            services.Configure<StaticDataLoadingOptions>(o =>
            {
                var batchSize = EnvironmentVariableHelper.Get("Nakijin:StaticDataLoader:BatchSize", configuration);
                var dbCommandTimeOut = EnvironmentVariableHelper.Get("Nakijin:StaticDataLoader:DbCommandTimeOut", configuration);
                o.BatchSize = string.IsNullOrEmpty(batchSize) ? 1000 : int.Parse(batchSize);
                o.DbCommandTimeOut =  string.IsNullOrEmpty(dbCommandTimeOut) ? 300 : int.Parse(dbCommandTimeOut);
            });

            services.Configure<RequestLocalizationOptions>(o =>
            {
                o.DefaultRequestCulture = new RequestCulture("en");
                o.SupportedCultures = new[]
                {
                    new CultureInfo("en"),
                    new CultureInfo("ar"),
                    new CultureInfo("ru")
                    //TODO: add others if needed
                };

                o.RequestCultureProviders.Insert(0, new RouteDataRequestCultureProvider { Options = o });
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
            var authorityUrl = authorityOptions["authorityUrl"];
            
            services.Configure<TokenRequestOptions>(options =>
            {
                var uri = new Uri(new Uri(authorityUrl), "/connect/token");
                options.Address = uri.ToString();
                options.ClientId = clientOptions["clientId"];
                options.ClientSecret = clientOptions["clientSecret"];
                options.Scope = clientOptions["scope"];
                options.GrantType = OidcConstants.GrantTypes.ClientCredentials;
            });
            
            services.AddAuthentication(IdentityServerAuthenticationDefaults.AuthenticationScheme)
                .AddIdentityServerAuthentication(options =>
                {
                    options.Authority = authorityUrl;
                    options.ApiName = authorityOptions["apiName"];
                    options.RequireHttpsMetadata = true;
                    options.SupportedTokens = SupportedTokens.Jwt;
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