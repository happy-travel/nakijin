using HappyTravel.Nakijin.Api.Infrastructure.Environments;
using HappyTravel.Nakijin.Api.Services.StaticDataPublication;
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
            string syncTimeout;
            
            if (environment.IsLocal())
            {
                endpoint = configuration["StaticDataPublication:Redis:Endpoint"];
                port = configuration["StaticDataPublication:Redis:Port"];
                streamName = configuration["StaticDataPublication:Redis:StreamName"];
                syncTimeout = configuration["StaticDataPublication:Redis:SyncTimeout"];
            }
            else
            {
                var redisOptions = vaultClient.Get(configuration["StaticDataPublication:Redis"]).GetAwaiter().GetResult();
                endpoint = redisOptions["endpoint"];
                port = redisOptions["port"];
                streamName = redisOptions["streamName"];
                syncTimeout = redisOptions["syncTimeout"];
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
                    },
                    SyncTimeout = int.Parse(syncTimeout)
                });
            services.Configure<StaticDataPublicationOptions>(o =>
            {
                o.StreamName = streamName;
            });
            
            services.AddSingleton<IStaticDataPublicationService, StaticDataPublicationService>();

            services.AddSingleton<AccommodationChangePublisher>();
            services.AddSingleton<LocationChangePublisher>();
            
            return services;
        }
    }
}