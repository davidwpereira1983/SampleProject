using System;
using System.IO;
using System.Reflection;
using Azure;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using DbUp;
using Microsoft.Extensions.Configuration;

namespace Company.TestProject.DbUpdate
{
    public class Program
    {
        public static int Main(string[] args)
        {
            Console.WriteLine("***********************************************************");
            Console.WriteLine("                     DB UPDATE                             ");
            Console.WriteLine("***********************************************************");

            var baseDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            var configuration = BuildConfiguration(args, baseDirectory);

            var connectionString = configuration.GetConnectionString("Main");

            Console.WriteLine($" ConnectionString: {connectionString}");
            string databaseScriptFolders = null;

            if (args != null && args.Length > 0)
            {
                databaseScriptFolders = Path.GetFullPath(args[0]);
            }
            else
            {
                databaseScriptFolders = Path.GetFullPath(Path.Combine(baseDirectory, "..\\..\\..\\..\\..\\Database\\"));
            }

            Console.WriteLine($"Database script folder: {databaseScriptFolders}");

            var upgrader =
                DeployChanges.To
                    .SqlDatabase(connectionString)
                    .WithScriptsFromFileSystem(databaseScriptFolders)
                    .WithTransaction()
                    .LogToConsole()
                    .Build();

            var result = upgrader.PerformUpgrade();

            if (!result.Successful)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(result.Error);
                Console.ResetColor();
#if DEBUG
                Console.ReadLine();
#endif
                return -1;
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Success!");
            Console.ResetColor();
            return 0;
        }

        private static IConfigurationRoot BuildConfiguration(string[] args, string baseDirectory)
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            Console.WriteLine($"Hosting environment: {environment}");
            Console.WriteLine($"Configuration path: {baseDirectory}");

            var builder = new ConfigurationBuilder();
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

            var configuration = builder.Build();
            return configuration;
        }
    }
}
