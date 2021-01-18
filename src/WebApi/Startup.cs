using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Threading.Tasks;
using AutoMapper;
using Company.TestProject.DataAccess;
using Company.TestProject.Service;
using Company.TestProject.Shared;
using Company.TestProject.WebApi.HealthCheck;
using Company.TestProject.WebApiClient.DTO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Microsoft.OpenTelemetry.Exporter.AzureMonitor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Company.TestProject.WebApi
{
    public class Startup
    {
        private readonly IResourceProvider resourceProvider;

        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            this.Configuration = configuration;
            this.Env = env;
            this.resourceProvider = new ResourceProvider();
        }

        public IConfiguration Configuration { get; }
        public IWebHostEnvironment Env { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            this.SetupTracing(services);

            services.AddHealthChecks()
                .AddSqlServer(this.Configuration["ConnectionStrings:Main"])
                .AddMemoryHealthCheck("memory");

            this.SetupDependencyInjection(services);

            services.AddControllers((op) =>
            {
                op.Filters.Add(new ProducesResponseTypeAttribute(typeof(List<ErrorDetail>), (int)HttpStatusCode.BadRequest));
                op.Filters.Add(new ProducesResponseTypeAttribute(typeof(ExceptionDetail), (int)HttpStatusCode.InternalServerError));
            }).AddNewtonsoftJson();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Company.TestProject.WebApi", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<ExceptionMiddleware> logger)
        {
            app.UseRequestResponseLogging();

            if (env.IsDevelopment() || env.EnvironmentName == "Local" || env.IsStaging())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Company.TestProject.WebApi v1"));
            }

            app.UseHealthChecks("/health", new HealthCheckOptions()
            {
                ResponseWriter = this.WriteResponseAsync
            });

            app.UseMiddleware<ExceptionMiddleware>(this.resourceProvider, logger);

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        private void SetupDependencyInjection(IServiceCollection services)
        {
            string connectionString = this.Configuration.GetConnectionString("Main");
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<IProductRepository>(o => new ProductRepository(connectionString));

            // Auto Mapper Configurations
            var mapperConfig = new MapperConfiguration(mc =>
            {
                mc.AddProfile(new MappingProfile());
            });

            IMapper mapper = mapperConfig.CreateMapper();
            services.AddSingleton(mapper);
        }

        private Task WriteResponseAsync(HttpContext httpContext, HealthReport result)
        {
            httpContext.Response.ContentType = "application/json";
            var defaultLogLevel = this.Configuration["Logging:LogLevel:Default"];
            var connectionString = this.Configuration["ConnectionStrings:Main"];
            var connectionStringBuilder = new SqlConnectionStringBuilder(connectionString);

            if (!string.IsNullOrEmpty(connectionStringBuilder.Password))
            {
                connectionStringBuilder.Password = $"{connectionStringBuilder.Password[0]}***{connectionStringBuilder.Password[connectionStringBuilder.Password.Length - 1]}";
            }

            var json = new JObject(
                new JProperty("status", result.Status.ToString()),
                new JProperty("configuration", new JObject(
                    new JProperty("connectionString", connectionStringBuilder.ToString()),
                    new JProperty("defaultLogLevel", defaultLogLevel),
                    new JProperty("hostEnvironment", this.Env.EnvironmentName))),
                new JProperty("results", new JObject(result.Entries.Select(pair =>
                    new JProperty(pair.Key, new JObject(
                        new JProperty("status", pair.Value.Status.ToString()),
                        new JProperty("description", pair.Value.Description),
                        new JProperty("data", new JObject(pair.Value.Data.Select(
                            p => new JProperty(p.Key, p.Value))))))))));

            return httpContext.Response.WriteAsync(json.ToString(Formatting.Indented));
        }

        private void SetupTracing(IServiceCollection services)
        {
            var exporter = this.Configuration.GetValue<string>("UseExporter").ToLowerInvariant();
            switch (exporter.ToLower())
            {
                case "jaeger":
                    services.AddOpenTelemetryTracing((builder) => builder
                        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(this.Configuration.GetValue<string>("Jaeger:ServiceName")))
                        .AddSource(ProductService.ActivitySourceName)
                        .AddSource(ProductRepository.ActivitySourceName)
                        .AddAspNetCoreInstrumentation((options) => options.Enrich = (activity, eventName, rawObject) =>
                        {
                            if (eventName.Equals("OnStartActivity"))
                            {
                                if (rawObject is HttpRequest httpRequest)
                                {
                                    activity.SetTag("requestProtocol", httpRequest.Protocol);
                                }
                            }
                            else if (eventName.Equals("OnStopActivity"))
                            {
                                if (rawObject is HttpResponse httpResponse)
                                {
                                    activity.SetTag("responseLength", httpResponse.ContentLength);
                                }
                            }
                        })
                        .AddHttpClientInstrumentation()
                        .AddGrpcClientInstrumentation()
                        .AddSqlClientInstrumentation(options =>
                        {
                            options.EnableConnectionLevelAttributes = true;
                            options.SetTextCommandContent = true;
                        })
                        .AddJaegerExporter(jaegerOptions =>
                        {
                            jaegerOptions.AgentHost = this.Configuration.GetValue<string>("Jaeger:Host");
                            jaegerOptions.AgentPort = this.Configuration.GetValue<int>("Jaeger:Port");
                        }));
                    break;
                case "azure":
                    services.AddOpenTelemetryTracing((builder) => builder
                        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(this.Configuration.GetValue<string>("Azure:ServiceName")))
                        .AddSource(ProductService.ActivitySourceName)
                        .AddSource(ProductRepository.ActivitySourceName)
                        .AddAspNetCoreInstrumentation((options) => options.Enrich = (activity, eventName, rawObject) =>
                        {
                            if (eventName.Equals("OnStartActivity"))
                            {
                                if (rawObject is HttpRequest httpRequest)
                                {
                                    activity.SetTag("requestProtocol", httpRequest.Protocol);
                                }
                            }
                            else if (eventName.Equals("OnStopActivity"))
                            {
                                if (rawObject is HttpResponse httpResponse)
                                {
                                    activity.SetTag("responseLength", httpResponse.ContentLength);
                                }
                            }
                        })
                        .AddHttpClientInstrumentation()
                        .AddGrpcClientInstrumentation()
                        .AddSqlClientInstrumentation(options =>
                        {
                            options.EnableConnectionLevelAttributes = true;
                            options.SetTextCommandContent = true;
                        })
                        .AddProcessor(new BatchExportProcessor<Activity>(new AzureMonitorTraceExporter(new AzureMonitorExporterOptions
                        {
                            ConnectionString = this.Configuration.GetValue<string>("Azure:ConnectionString")
                        }))));
                    break;
                case "otlp":
                    services.AddOpenTelemetryTracing((builder) => builder
                        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(this.Configuration.GetValue<string>("Otlp:ServiceName")))
                        .AddSource(ProductService.ActivitySourceName)
                        .AddSource(ProductRepository.ActivitySourceName)
                        .AddAspNetCoreInstrumentation((options) => options.Enrich
                        = (activity, eventName, rawObject) =>
                        {
                            if (eventName.Equals("OnStartActivity"))
                            {
                                if (rawObject is HttpRequest httpRequest)
                                {
                                    activity.SetTag("requestProtocol", httpRequest.Protocol);
                                }
                            }
                            else if (eventName.Equals("OnStopActivity"))
                            {
                                if (rawObject is HttpResponse httpResponse)
                                {
                                    activity.SetTag("responseLength", httpResponse.ContentLength);
                                }
                            }
                        })
                        .AddHttpClientInstrumentation()
                        .AddGrpcClientInstrumentation()
                        .AddSqlClientInstrumentation(options =>
                        {
                            options.EnableConnectionLevelAttributes = true;
                            options.SetTextCommandContent = true;
                        })
                        .AddOtlpExporter(otlpOptions =>
                        {
                            otlpOptions.Endpoint = this.Configuration.GetValue<string>("Otlp:Endpoint");
                        }));
                    break;
                default:
                    services.AddOpenTelemetryTracing((builder) => builder
                        .AddSource(ProductService.ActivitySourceName)
                        .AddSource(ProductRepository.ActivitySourceName)
                        .AddAspNetCoreInstrumentation((options) => options.Enrich
                        = (activity, eventName, rawObject) =>
                        {
                            if (eventName.Equals("OnStartActivity"))
                            {
                                if (rawObject is HttpRequest httpRequest)
                                {
                                    activity.SetTag("requestProtocol", httpRequest.Protocol);
                                }
                            }
                            else if (eventName.Equals("OnStopActivity"))
                            {
                                if (rawObject is HttpResponse httpResponse)
                                {
                                    activity.SetTag("responseLength", httpResponse.ContentLength);
                                }
                            }
                        })
                        .AddHttpClientInstrumentation()
                        .AddGrpcClientInstrumentation()
                        .AddSqlClientInstrumentation(options =>
                        {
                            options.EnableConnectionLevelAttributes = true;
                            options.SetTextCommandContent = true;
                        })
                        .AddConsoleExporter());
                    break;
            }
        }
    }
}
