using System;
using System.IO;
using System.Reflection;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Company.TestProject.WebApi
{
    public class Program
    {
        public static int Main(string[] args)
        {
            var baseDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            IConfiguration configuration = BuildConfiguration(args, baseDirectory);

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.With<TraceIdentifierEnricher>()
                .CreateLogger();

            try
            {
                Log.Information("Getting the motors running...");
                CreateHostBuilder(args, baseDirectory).Build().Run();
                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args, string baseDirectory) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>()
                    .ConfigureAppConfiguration((builderContext, config) =>
                    {
                        var env = builderContext.HostingEnvironment;
                        SetupConfigurationBuilder(args, baseDirectory, env.EnvironmentName, config);
                    });
                });

        private static IConfigurationRoot BuildConfiguration(string[] args, string baseDirectory)
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            Console.WriteLine($"Hosting environment: {environment}");
            Console.WriteLine($"Configuration path: {baseDirectory}");

            var builder = new ConfigurationBuilder();
            SetupConfigurationBuilder(args, baseDirectory, environment, builder);

            var configuration = builder.Build();
            return configuration;
        }

        private static void SetupConfigurationBuilder(string[] args, string baseDirectory, string environment, IConfigurationBuilder builder)
        {
            builder
                .SetBasePath(baseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
                .AddJsonFile("appsettings.overrides.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.overrides.{environment}.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.overrides.personal.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables(prefix: "Company_TestProject_")
                .AddCommandLine(args);

            var tempConfiguration = builder.Build();
            var azureConfiguration = tempConfiguration.GetSection("AzureKeyVault")?.Get<AzureKeyVaultConfiguration>();

            if (azureConfiguration != null && azureConfiguration.Enable)
            {
                builder.AddAzureKeyVault(
                    new SecretClient(new Uri(azureConfiguration.Url), new DefaultAzureCredential(true)),
                    new AzureKeyVaultConfigurationOptions
                    {
                        Manager = new PrefixKeyVaultSecretManager(azureConfiguration.Prefix),
                        ReloadInterval = TimeSpan.FromMinutes(azureConfiguration.ReloadIntervalInMinutes)
                    });
            }
        }
    }
}
