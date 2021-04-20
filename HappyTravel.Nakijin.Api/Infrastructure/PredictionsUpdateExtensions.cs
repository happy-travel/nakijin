using System;
using HappyTravel.Nakijin.Api.Infrastructure.Environments;
using HappyTravel.Nakijin.Api.Services.PredictionsUpdate;
using HappyTravel.VaultClient;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis.Extensions.Core.Configuration;

namespace HappyTravel.Nakijin.Api.Infrastructure
{
    public static class PredictionsUpdateExtensions
    {
        public static IServiceCollection AddPredictionsUpdate(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
        {
            var role = configuration["PredictionsUpdate:Role"];
            using var vaultClient = new VaultClient.VaultClient(new VaultOptions
            {
                BaseUrl = new Uri(EnvironmentVariableHelper.Get("Vault:Endpoint", configuration)),
                Engine = configuration["Vault:Engine"],
                Role = role
            });
            var token = configuration[configuration["Vault:Token"]];
            vaultClient.Login(token).GetAwaiter().GetResult();
            var redisOptions = vaultClient.Get(configuration["PredictionsUpdate:Redis"]).GetAwaiter().GetResult();
            string endpoint;
            string port;
            string streamName;
            if (environment.IsLocal())
            {
                endpoint = configuration["PredictionsUpdate:Redis:Endpoint"];
                port = configuration["PredictionsUpdate:Redis:Port"];
                streamName = configuration["PredictionsUpdate:Redis:StreamName"];
            }
            else
            {
                endpoint = redisOptions["endpoint"];
                port = redisOptions["port"];
                streamName = redisOptions["streamName"];
            }
            
            services.AddStackExchangeRedisExtensions<StackExchangeRedisDefaultSerializer>(s 
                => new ()
                {
                    Hosts = new []
                    {
                        new RedisHost
                        {
                            Host = endpoint,
                            Port = int.Parse(port)
                        }
                    }
                });
            services.Configure<PredictionUpdateOptions>(o =>
            {
                o.StreamName = streamName;
            });
            
            services.AddSingleton<IPredictionsUpdateService, PredictionsUpdateService>();
            
            return services;
        }
    }
}