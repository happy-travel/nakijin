using HappyTravel.Nakijin.Api.Infrastructure.Environments;
using HappyTravel.Nakijin.Api.Services.PredictionsUpdate;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis.Extensions.Core.Configuration;

namespace HappyTravel.Nakijin.Api.Infrastructure
{
    public static class StaticDataPublicationExtensions
    {
        public static IServiceCollection AddStaticDataPublicationService(this IServiceCollection services, VaultClient.VaultClient vaultClient, IConfiguration configuration, IWebHostEnvironment environment)
        {
            string endpoint;
            string port;
            string streamName;
            if (environment.IsLocal())
            {
                endpoint = configuration["StaticDataPublication:Redis:Endpoint"];
                port = configuration["StaticDataPublication:Redis:Port"];
                streamName = configuration["StaticDataPublication:Redis:StreamName"];
            }
            else
            {
                var redisOptions = vaultClient.Get(configuration["StaticDataPublication:Redis"]).GetAwaiter().GetResult();
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
            services.Configure<StaticDataPublicationOptions>(o =>
            {
                o.StreamName = streamName;
            });
            
            services.AddSingleton<IStaticDataPublicationService, StaticDataPublicationService>();
            
            return services;
        }
    }
}