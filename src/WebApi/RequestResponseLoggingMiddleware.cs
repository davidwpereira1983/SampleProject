using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.IO;

namespace Company.TestProject.WebApi
{
    public class RequestResponseLoggingMiddleware
    {
        private readonly RequestDelegate next;
        private readonly ILogger logger;
        private readonly RecyclableMemoryStreamManager recyclableMemoryStreamManager;
        public RequestResponseLoggingMiddleware(RequestDelegate next, ILoggerFactory loggerFactory)
        {
            this.next = next;
            this.logger = loggerFactory.CreateLogger<RequestResponseLoggingMiddleware>();
            this.recyclableMemoryStreamManager = new RecyclableMemoryStreamManager();
        }

        public async Task InvokeAsync(HttpContext context)
        {
            await this.LogRequestAsync(context).ConfigureAwait(false);
            await this.LogResponseAsync(context).ConfigureAwait(false);
        }

        private static string ReadStreamInChunks(Stream stream)
        {
            const int readChunkBufferLength = 4096;
            stream.Seek(0, SeekOrigin.Begin);
            using var textWriter = new StringWriter();
            using var reader = new StreamReader(stream);
            var readChunk = new char[readChunkBufferLength];
            int readChunkLength;
            do
            {
                readChunkLength = reader.ReadBlock(
                    readChunk,
                    0,
                    readChunkBufferLength);
                textWriter.Write(readChunk, 0, readChunkLength);
            }
            while (readChunkLength > 0);
            return textWriter.ToString();
        }

        private async Task LogRequestAsync(HttpContext context)
        {
            if (context.Request.Path.HasValue &&
                context.Request.Path.Value.IndexOf("swagger", StringComparison.InvariantCultureIgnoreCase) < 0)
            {
                context.Request.EnableBuffering();
                await using var requestStream = this.recyclableMemoryStreamManager.GetStream();
                await context.Request.Body.CopyToAsync(requestStream).ConfigureAwait(false);
                this.logger.LogInformation($"Http Request Information | {context.Request.Method} {context.Request.Path} | " +
                                           $"Schema: ({context.Request.Scheme}), " +
                                           $"Host: ({context.Request.Host}), " +
                                           $"Path: ({context.Request.Path}), " +
                                           $"QueryString: ({context.Request.QueryString}), " +
                                           $"Request Body: ({ReadStreamInChunks(requestStream)})");

                context.Request.Body.Position = 0;
            }
        }

        private async Task LogResponseAsync(HttpContext context)
        {
            if (context.Request.Path.HasValue &&
                context.Request.Path.Value.IndexOf("swagger", StringComparison.InvariantCultureIgnoreCase) < 0)
            {
                var activityTraceId = Activity.Current.TraceId;
                var originalBodyStream = context.Response.Body;
                context.Response.Headers.Add("traceId", new StringValues(activityTraceId.ToString()));
                await using var responseBody = this.recyclableMemoryStreamManager.GetStream();
                context.Response.Body = responseBody;
                await this.next(context).ConfigureAwait(false);
                context.Response.Body.Seek(0, SeekOrigin.Begin);
                var text = await new StreamReader(context.Response.Body).ReadToEndAsync().ConfigureAwait(false);
                context.Response.Body.Seek(0, SeekOrigin.Begin);
                this.logger.LogInformation($"Http Response Information | {context.Request.Method} {context.Request.Path} | {context.Response.StatusCode} | " +
                                           $"Schema: ({context.Request.Scheme}), " +
                                           $"Host: ({context.Request.Host}), " +
                                           $"Path: ({context.Request.Path}), " +
                                           $"QueryString: ({context.Request.QueryString}), " +
                                           $"Response Body: ({text})");

                await responseBody.CopyToAsync(originalBodyStream).ConfigureAwait(false);
            }
            else
            {
                await this.next(context).ConfigureAwait(false);
            }
        }
    }
}
