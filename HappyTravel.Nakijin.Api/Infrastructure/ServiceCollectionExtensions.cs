﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using HappyTravel.LocationNameNormalizer.Extensions;
using HappyTravel.Nakijin.Data;
using HappyTravel.Nakijin.Api.Infrastructure.Environments;
using HappyTravel.Nakijin.Api.Services;
using HappyTravel.Nakijin.Api.Services.Validators;
using HappyTravel.Nakijin.Api.Services.Workers;
using HappyTravel.Nakijin.Api.Services.Workers.AccommodationDataCalculation;
using HappyTravel.Nakijin.Api.Services.Workers.AccommodationMapping;
using HappyTravel.Nakijin.Api.Services.Workers.LocationMapping;
using HappyTravel.SuppliersCatalog;
using HappyTravel.Nakijin.Api.Services.Workers.Preloading;
using IdentityModel.Client;
using IdentityServer4.AccessTokenValidation;
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

namespace HappyTravel.Nakijin.Api.Infrastructure
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddServices(this IServiceCollection services)
        {
            services.AddTransient<IAccommodationPreloader, AccommodationPreloader>();
            services.AddTransient<IAccommodationMapper, AccommodationMapper>();
            services.AddTransient<IAccommodationMapperDataRetrieveService, AccommodationMapperDataRetrieveService>();
            services.AddTransient<AccommodationMapperHelper>();
            services.AddTransient<IAccommodationDataMerger, AccommodationDataMerger>();
            services.AddTransient<AccommodationMergerHelper>();
            services.AddTransient<ILocationMapper, LocationMapper>();
            services.AddTransient<ICountryMapper, CountryMapper>();
            services.AddTransient<ILocalityMapper, LocalityMapper>();
            services.AddTransient<ILocalityZoneMapper, LocalityZoneMapper>();
            services.AddTransient<ILocationMapperDataRetrieveService, LocationsMapperDataRetrieveService>();
            services.AddTransient<IAccommodationManagementService, AccommodationManagementService>();
            services.AddTransient<ISuppliersPriorityService, SuppliersPriorityService>();
            services.AddTransient<IConnectorClient, ConnectorClient>();

            services.AddTransient<IAccommodationService, AccommodationService>();
            services.AddTransient<ILocationService, LocationService>();

            services.AddTransient<ILocalityValidator, LocalityValidator>();
            services.AddSingleton<LocationNameRetriever>();
                        
            services.AddNameNormalizationServices();
            services.AddSingleton<MultilingualDataHelper>();
            services.AddTransient<AccommodationMappingsCache>();
            
            services.AddTransient<ILocationManagementService, LocationManagementService>();

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
                var preloadingBatchSize =
                    EnvironmentVariableHelper.Get("Nakijin:StaticDataLoader:PreloadingBatchSize", configuration);
                var mappingBatchSize =
                    EnvironmentVariableHelper.Get("Nakijin:StaticDataLoader:MappingBatchSize", configuration);
                var mergingBatchSize =
                    EnvironmentVariableHelper.Get("Nakijin:StaticDataLoader:MergingBatchSize", configuration);
                var dbCommandTimeOut =
                    EnvironmentVariableHelper.Get("Nakijin:StaticDataLoader:DbCommandTimeOut", configuration);

                o.PreloadingBatchSize = string.IsNullOrEmpty(preloadingBatchSize)
                    ? Models.Constants.DefaultPreloadingBatchSize
                    : int.Parse(preloadingBatchSize);
                o.MappingBatchSize = string.IsNullOrEmpty(mappingBatchSize)
                    ? Models.Constants.DefaultMappingBatchSize
                    : int.Parse(mappingBatchSize);
                o.MergingBatchSize = string.IsNullOrEmpty(mergingBatchSize)
                    ? Models.Constants.DefaultMergingBatchSize
                    : int.Parse(mergingBatchSize);
                o.DbCommandTimeOut = string.IsNullOrEmpty(dbCommandTimeOut)
                    ? Models.Constants.DefaultDbCommandTimeOut
                    : int.Parse(dbCommandTimeOut);
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

                o.RequestCultureProviders.Insert(0, new RouteDataRequestCultureProvider {Options = o});
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

            services.AddAccessTokenManagement(options =>
            {
                options.Client.Clients.Add(HttpClientNames.Identity, new ClientCredentialsTokenRequest
                {
                    Address = new Uri(new Uri(authorityUrl), "/connect/token").ToString(),
                    ClientId = clientOptions["clientId"],
                    ClientSecret = clientOptions["clientSecret"],
                    Scope = clientOptions["scope"]
                });
            });

            services.AddClientAccessTokenClient(HttpClientNames.Connectors, HttpClientNames.Identity);

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