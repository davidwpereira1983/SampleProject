using System;
using System.IO;
using System.Reflection;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;

namespace Company.TestProject.Tests.Integration
{
    public abstract class IntegrationTestBase
    {
        protected IConfigurationRoot configuration;
        protected string connectionString;

        protected IntegrationTestBase()
        {
            this.configuration = this.BuildConfiguration(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            this.connectionString = this.configuration.GetConnectionString("main");
            NUnit.Framework.TestContext.Progress.WriteLine($"ConnectionString: {this.connectionString}");
        }

        private IConfigurationRoot BuildConfiguration(string baseDirectory)
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            if (!environment.Contains("IntegrationTests"))
            {
                environment = "IntegrationTests";
            }

            NUnit.Framework.TestContext.Progress.WriteLine($"Hosting environment: {environment}");
            NUnit.Framework.TestContext.Progress.WriteLine($"Base directory: {baseDirectory}");

            var builder = new ConfigurationBuilder();
            builder
                .SetBasePath(baseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
                .AddJsonFile("appsettings.overrides.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.overrides.{environment}.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.overrides.personal.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables(prefix: "Company_TestProject_");

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

            var configuration = builder.Build();
            return configuration;
        }
    }
}
