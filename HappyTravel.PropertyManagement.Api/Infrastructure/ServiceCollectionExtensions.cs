using System;
using System.Net.Http;
using HappyTravel.PropertyManagement.Api.Infrastructure.Environments;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace.Configuration;
using OpenTelemetry.Trace.Samplers;
using Polly;
using Polly.Extensions.Http;

namespace HappyTravel.PropertyManagement.Api.Infrastructure
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddHttpClients(this IServiceCollection services)
        {
            /*services.AddHttpClient("")
                .SetHandlerLifetime(TimeSpan.FromMinutes(0))
                .AddPolicyHandler(GetDefaultRetryPolicy());*/

            return services;
        }


        public static IServiceCollection AddServices(this IServiceCollection services)
        {
            return services;
        }


        public static IServiceCollection AddTracing(this IServiceCollection services, IWebHostEnvironment environment, IConfiguration configuration)
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

            var serviceName = $"{environment.ApplicationName}-{environment.EnvironmentName}";
            services.AddOpenTelemetrySdk(builder =>
            {
                builder.AddRequestInstrumentation()
                    .AddDependencyInstrumentation()
                    .UseJaegerActivityExporter(options =>
                    {
                        options.ServiceName = serviceName;
                        options.AgentHost = agentHost;
                        options.AgentPort = agentPort;
                    })
                    .SetResource(Resources.CreateServiceResource(serviceName))
                    .SetSampler(new AlwaysOnActivitySampler());
            });

            return services;
        }


        public static IServiceCollection ConfigureServiceOptions(this IServiceCollection services, IConfiguration configuration)
        {
            return services;
        }


        private static IAsyncPolicy<HttpResponseMessage> GetDefaultRetryPolicy()
        {
            var jitter = new Random();

            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(3, attempt
                    => TimeSpan.FromMilliseconds(Math.Pow(500, attempt)) + TimeSpan.FromMilliseconds(jitter.Next(0, 100)));
        }
    }
}
