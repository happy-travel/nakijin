using System;
using System.Diagnostics;
using HappyTravel.ConsulKeyValueClient.ConfigurationProvider.Extensions;
using HappyTravel.Nakijin.Api.Infrastructure.Environments;
using HappyTravel.StdOutLogger.Extensions;
using HappyTravel.StdOutLogger.Infrastructure;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HappyTravel.Nakijin.Api
{
    public class Program
    {
        public static void Main(string[] args) => CreateHostBuilder(args).Build().Run();


        public static IWebHostBuilder CreateHostBuilder(string[] args)
            => WebHost.CreateDefaultBuilder(args)
                .UseKestrel()
                .UseStartup<Startup>()
                .UseSentry(options =>
                {
                    options.Dsn = Environment.GetEnvironmentVariable("HTDC_NAKIJIN_SENTRY_ENDPOINT");
                    options.Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                    options.IncludeActivityData = true;
                    options.BeforeSend = sentryEvent =>
                    {
                        foreach (var (key, value) in OpenTelemetry.Baggage.Current)
                            sentryEvent.SetTag(key, value);
                                    
                        sentryEvent.SetTag("TraceId", Activity.Current?.TraceId.ToString() ?? string.Empty);
                        sentryEvent.SetTag("SpanId", Activity.Current?.SpanId.ToString() ?? string.Empty);

                        return sentryEvent;
                    };
                })
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    var environment = hostingContext.HostingEnvironment;

                    config.AddJsonFile("appsettings.json", false, true)
                        .AddJsonFile($"appsettings.{environment.EnvironmentName}.json", true, true);
                    config.AddEnvironmentVariables();
                    config.AddConsulKeyValueClient(Environment.GetEnvironmentVariable("CONSUL_HTTP_ADDR") ?? throw new InvalidOperationException("Consul endpoint is not set"),
                        "nakijin",
                        Environment.GetEnvironmentVariable("CONSUL_HTTP_TOKEN") ?? throw new InvalidOperationException("Consul http token is not set"));
                })
                .ConfigureLogging((hostingContext, logging) =>
                {
                    logging.ClearProviders()
                        .AddConfiguration(hostingContext.Configuration.GetSection("Logging"));

                    var env = hostingContext.HostingEnvironment;
                    if (env.IsLocal())
                        logging.AddConsole();
                    else
                        logging
                            .AddStdOutLogger(options =>
                            {
                                options.IncludeScopes = true;
                                options.RequestIdHeader = Constants.DefaultRequestIdHeader;
                                options.UseUtcTimestamp = true;
                            })
                            .AddSentry();
                })
                .UseSetting(WebHostDefaults.SuppressStatusMessagesKey, "true");
    }
}