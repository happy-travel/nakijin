using System;
using Microsoft.Extensions.DependencyInjection;

namespace HappyTravel.SecurityClient
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSecurityTokenManager(this IServiceCollection service, string authorityUrl)
        {
            service.AddHttpClient<SecurityTokenManager>(c => c.BaseAddress = new Uri(authorityUrl));

            service.AddSingleton<ISecurityTokenManager, SecurityTokenManager>();

            return service;
        }
    }
}
