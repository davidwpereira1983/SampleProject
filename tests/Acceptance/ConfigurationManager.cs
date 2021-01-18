using System;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace Company.TestProject.Tests.Acceptance
{
    public static class ConfigurationManager
    {
        static ConfigurationManager()
        {
            var baseDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Configuration = BuildConfiguration(baseDirectory);
        }

        public static IConfigurationRoot Configuration { get; }

        private static IConfigurationRoot BuildConfiguration(string baseDirectory)
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            Console.WriteLine($"Hosting environment: {environment}");
            Console.WriteLine($"Configuration path: {baseDirectory}");

            var builder = new ConfigurationBuilder();
            var configuration = builder
                .SetBasePath(baseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
                .AddJsonFile("appsettings.overrides.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.overrides.{environment}.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.overrides.personal.json", optional: true, reloadOnChange: true)
                .Build();
            return configuration;
        }
    }
}
