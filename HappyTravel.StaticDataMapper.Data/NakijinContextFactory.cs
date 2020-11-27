using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HappyTravel.VaultClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace HappyTravel.StaticDataMapper.Data
{
    public class NakijinContextFactory : IDesignTimeDbContextFactory<NakijinContext>
    {
        public NakijinContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location))
                .AddJsonFile("appsettings.json", false, true)
                .Build();

            var dbOptions = GetDbOptions(configuration);

            var dbContextOptions = new DbContextOptionsBuilder<NakijinContext>();
            dbContextOptions.UseNpgsql(GetConnectionString(configuration, dbOptions), builder => builder.UseNetTopologySuite());
            var context = new NakijinContext(dbContextOptions.Options);

            return context;
        }


        private static string GetConnectionString(IConfiguration configuration, Dictionary<string, string> dbOptions)
            => string.Format(configuration["ConnectionStrings:Nakijin"],
                dbOptions["host"],
                dbOptions["port"],
                dbOptions["userId"],
                dbOptions["password"]);


        private static Dictionary<string, string> GetDbOptions(IConfiguration configuration)
        {
            using var vaultClient = new VaultClient.VaultClient(new VaultOptions
            {
                BaseUrl = new Uri(Environment.GetEnvironmentVariable(configuration["Vault:Endpoint"])!, UriKind.Absolute),
                Engine = configuration["Vault:Engine"],
                Role = configuration["Vault:Role"]
            });
            vaultClient.Login(Environment.GetEnvironmentVariable(configuration["Vault:Token"])).Wait();

            return vaultClient.Get(configuration["Nakijin:Database:Options"]).Result;
        }
    }
}
