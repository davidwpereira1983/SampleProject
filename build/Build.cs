using System;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

[CheckBuildProjectConfigurations]
[UnsetVisualStudioEnvironmentVariables]
class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode
    private static IConfigurationRoot configuration;

    public static int Main()
    {
        var baseDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        configuration = BuildConfiguration(baseDirectory);
        return Execute<Build>(x => x.Compile);
    }

    private static IConfigurationRoot BuildConfiguration(string baseDirectory)
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

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution] readonly Solution Solution;
    [GitRepository] readonly GitRepository GitRepository;
    [GitVersion(Framework = "netcoreapp3.1")] readonly GitVersion GitVersion;

    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath TestsDirectory => RootDirectory / "tests";
    AbsolutePath OutputDirectory => RootDirectory / "output";
    AbsolutePath TestResultDirectory => OutputDirectory / "test-results";

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
            TestsDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
            EnsureCleanDirectory(OutputDirectory);
        });

    Target Restore => _ => _
        .DependsOn(Clean)
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(Solution));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .SetAssemblyVersion(GitVersion.AssemblySemVer)
                .SetFileVersion(GitVersion.AssemblySemFileVer)
                .SetInformationalVersion(GitVersion.InformationalVersion)
                .EnableNoRestore());
        });

    Target Publish => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetPublish(p =>
            {
                var project = Solution.GetProject("Company.TestProject.WebApi");

                return p.SetProject(project)
                    .SetConfiguration("Release")
                    .SetOutput(OutputDirectory);
            });
        });

    Target Test => _ => _
        .DependsOn(Compile)
        .Produces(TestResultDirectory / "*.trx")
        .Executes(() =>
        {
            var project = Solution.GetProject("Company.TestProject.Unit");


            DotNetTest(_ => _
                .SetConfiguration(Configuration)
                .SetNoBuild(InvokedTargets.Contains(Compile))
                .ResetVerbosity()
                .SetResultsDirectory(TestResultDirectory)                              
                .CombineWith(new[] { project }, (_, v) => _
                    .SetProjectFile(v)
                    .SetLogger($"trx;LogFileName={v.Name}.trx")));            
        });

    Target SetupLocal => _ => _
        .DependsOn(SetLocalEnvironmentVariables, SetupLocalDockerContainer, CreateLocalDatabase, UpdateLocalDatabase);

    Target SetLocalEnvironmentVariables => _ => _
        .Executes(() =>
        {
            Console.WriteLine($"Setting environment variables\r\n");

            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            if (environment != "Local")
            {
                Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Local", EnvironmentVariableTarget.Machine);
            }
        });

    Target UpdateLocalDatabase => _ => _
        .Executes(() =>
        {
            var project = Solution.GetProject("Company.TestProject.DbUpdate");
            string profileDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Local", EnvironmentVariableTarget.Process);
            DotNet($"run --project {project}");

            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "AcceptanceTests", EnvironmentVariableTarget.Process);
            DotNet($"run --project {project}");

            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "IntegrationTests", EnvironmentVariableTarget.Process);
            DotNet($"run --project {project}");

        });    

    Target CreateLocalDatabase => _ => _
        .DependsOn(SetLocalEnvironmentVariables)
        .Executes(() =>
        {
            string connectionString = configuration.GetConnectionString("Main");

            Console.WriteLine($"Connection string from configuration: {connectionString}");

            var connectionStringBuilder = new SqlConnectionStringBuilder(connectionString);
            connectionStringBuilder.InitialCatalog = "master";

            Console.WriteLine($"Connection string: {connectionStringBuilder.ConnectionString}");

            SqlHelper.Execute(connectionStringBuilder.ConnectionString,
                @"IF DB_ID(N'TestProject') IS NULL
            BEGIN
	            CREATE DATABASE TestProject
            END");

            SqlHelper.Execute(connectionStringBuilder.ConnectionString,
                @"IF DB_ID(N'TestProject_AcceptanceTests') IS NULL
            BEGIN
	            CREATE DATABASE TestProject_AcceptanceTests
            END");

            SqlHelper.Execute(connectionStringBuilder.ConnectionString,
                @"IF DB_ID(N'TestProject_IntegrationTests') IS NULL
            BEGIN
	            CREATE DATABASE TestProject_IntegrationTests
            END");
        });

    Target DropLocalDatabase => _ => _
        .DependsOn(SetLocalEnvironmentVariables)
        .Executes(() =>
        {
            string connectionString = configuration.GetConnectionString("Main");

            var connectionStringBuilder = new SqlConnectionStringBuilder(connectionString);
            connectionStringBuilder.InitialCatalog = "master";

            SqlHelper.Execute(connectionStringBuilder.ConnectionString,
                @"IF DB_ID(N'TestProject') IS NOT NULL
                BEGIN
	                DROP DATABASE TestProject
                END");

            SqlHelper.Execute(connectionStringBuilder.ConnectionString,
                @"IF DB_ID(N'TestProject_AcceptanceTests') IS NOT NULL
                BEGIN
	                DROP DATABASE TestProject_AcceptanceTests
                END");

            SqlHelper.Execute(connectionStringBuilder.ConnectionString,
                @"IF DB_ID(N'TestProject_IntegrationTests') IS NOT NULL
                BEGIN
	                DROP DATABASE TestProject_IntegrationTests
                END");
        });

    Target ResetLocalDatabase => _ => _
        .DependsOn(SetupLocalDockerContainer, SetLocalEnvironmentVariables, DropLocalDatabase, CreateLocalDatabase, UpdateLocalDatabase);

    Target SetupLocalDockerContainer => _ => _
        .Executes(() =>
        {
            IProcess iprocess = ProcessTasks.StartProcess("docker-compose", "--file docker-compose-local.yml up -d", Path.Combine(RootDirectory, "docker\\"), null, null, false, null, null, null);
            ProcessExtensions.AssertZeroExitCode(iprocess);

            Console.WriteLine("Waiting for the container to start");
            Thread.Sleep(20000);
        });

}
